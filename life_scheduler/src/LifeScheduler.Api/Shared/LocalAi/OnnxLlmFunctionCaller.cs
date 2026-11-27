using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace LifeScheduler.Api.Shared.LocalAi;

public sealed class OnnxLlmFunctionCaller : ILlmFunctionCaller, IDisposable
{
    private readonly Model _model;
    private readonly Tokenizer _tokenizer;
    private readonly LocalAiOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<OnnxLlmFunctionCaller>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Generator? _generator;
    private int _prefixLength;
    private DateOnly _prefixDate;
    private string? _prefixSchema;

    public OnnxLlmFunctionCaller(
        IOptions<LocalAiOptions> options,
        IHostEnvironment environment,
        TimeProvider time,
        ILogger<OnnxLlmFunctionCaller>? logger = null)
        : this(options.Value, environment.ContentRootPath, time, logger)
    {
    }

    public OnnxLlmFunctionCaller(
        LocalAiOptions options,
        string contentRoot,
        TimeProvider time,
        ILogger<OnnxLlmFunctionCaller>? logger = null)
    {
        _options = options;
        _time = time;
        _logger = logger;

        var path = LocalAiOptions.ResolvePath(contentRoot, options.OnnxModelPath);

        if (!Directory.Exists(path))
        {
            throw new InvalidOperationException(
                $"ONNX-модель не найдена: {path}. Положите папку phi-4-mini-onnx-cpu-int4 " +
                "в src/LifeScheduler.Api/models/ и см. README.md.");
        }

        if (!File.Exists(Path.Combine(path, "genai_config.json")))
        {
            throw new InvalidOperationException(
                $"В папке ONNX-модели нет genai_config.json: {path}. См. README.md.");
        }

        if (path.Any(symbol => symbol > 127))
        {
            throw new InvalidOperationException(
                $"Путь к ONNX-модели должен быть ASCII (без кириллицы): {path}. " +
                "Guidance-токенизатор ONNX Runtime GenAI не открывает не-ASCII пути.");
        }

        try
        {
            _model = new Model(path);
            _tokenizer = new Tokenizer(_model);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Не удалось загрузить ONNX-модель из {path}. См. README.md.", ex);
        }

        logger?.LogInformation("ONNX-модель загружена: {Path}", path);
    }

    public async Task<LlmToolCall> CallAsync(
        string userText,
        string toolsSchema,
        CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var total = Stopwatch.StartNew();
            var localNow = _time.GetLocalNow();
            var today = DateOnly.FromDateTime(localNow.DateTime);
            var systemPrompt = BuildSystemPrompt(toolsSchema, localNow);
            var responseSchema = BuildResponseSchema(toolsSchema);

            // Системная часть одинакова в пределах дня, поэтому генерируем её один раз
            // и переиспользуем KV-кеш: на запрос остаётся префилл только хвоста с фразой.
            var conversation = JsonSerializer.Serialize(new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = Sanitize(userText) },
            });

            // Ровно тот блок, который chat template выдаёт для system-сообщения без tools.
            // Проверка IsPrefix ниже отбрасывает кеш, если шаблон модели изменится.
            var prefixPrompt = $"<|system|>{systemPrompt}<|end|>";
            var prompt = _tokenizer.ApplyChatTemplate(
                string.Empty,
                conversation,
                string.Empty,
                true);

            using var prefixSequences = _tokenizer.Encode(prefixPrompt);
            using var sequences = _tokenizer.Encode(prompt);

            var prefixTokens = prefixSequences[0];
            var promptTokens = sequences[0];
            var reusable = IsPrefix(prefixTokens, promptTokens);

            var cacheHit = reusable
                           && _generator is not null
                           && _prefixDate == today
                           && _prefixLength == prefixTokens.Length
                           && string.Equals(_prefixSchema, responseSchema, StringComparison.Ordinal);

            var prefill = Stopwatch.StartNew();

            if (!cacheHit)
            {
                ResetGenerator(
                    reusable ? prefixPrompt : prompt,
                    responseSchema,
                    today,
                    reusable ? prefixTokens.Length : 0);
            }

            if (reusable)
            {
                var generator = _generator!;
                generator.RewindTo((ulong)_prefixLength);

                if (promptTokens.Length > _prefixLength)
                {
                    generator.AppendTokens(promptTokens[_prefixLength..]);
                }
            }

            prefill.Stop();

            var active = _generator!;
            var decode = Stopwatch.StartNew();
            using var stream = _tokenizer.CreateStream();

            var raw = new StringBuilder();
            var generated = 0;

            while (!active.IsDone())
            {
                active.GenerateNextToken();

                if (active.IsDone())
                {
                    break;
                }

                raw.Append(stream.Decode(active.GetNextTokens()[0]));
                generated++;
            }

            decode.Stop();
            total.Stop();

            _logger?.LogInformation(
                "voice.metrics stage=llm cache={Cache} prompt_chars={PromptChars} " +
                "prompt_tokens={PromptTokens} prefix_tokens={PrefixTokens} prefill_ms={PrefillMs} " +
                "decode_ms={DecodeMs} gen_tokens={GenTokens} total_ms={TotalMs}",
                cacheHit ? "hit" : "miss",
                prompt.Length,
                promptTokens.Length,
                _prefixLength,
                prefill.ElapsedMilliseconds,
                decode.ElapsedMilliseconds,
                generated,
                total.ElapsedMilliseconds);

            return ParseToolCall(raw.ToString());
        }
        catch (LlmOutputException)
        {
            throw;
        }
        catch
        {
            _generator?.Dispose();
            _generator = null;
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ResetGenerator(
        string promptText,
        string responseSchema,
        DateOnly today,
        int prefixLength)
    {
        _generator?.Dispose();
        _generator = null;

        using var parameters = new GeneratorParams(_model);
        parameters.SetSearchOption("max_length", Math.Max(1024, _options.OnnxMaxLength));
        parameters.SetGuidance("json_schema", responseSchema);

        var generator = new Generator(_model, parameters);

        try
        {
            using var sequences = _tokenizer.Encode(promptText);
            generator.AppendTokenSequences(sequences);
        }
        catch
        {
            generator.Dispose();
            throw;
        }

        _generator = generator;
        _prefixLength = prefixLength;
        _prefixDate = today;
        _prefixSchema = responseSchema;
    }

    private static bool IsPrefix(ReadOnlySpan<int> prefix, ReadOnlySpan<int> tokens)
    {
        if (prefix.Length >= tokens.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (prefix[i] != tokens[i])
            {
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        _generator?.Dispose();
        _tokenizer.Dispose();
        _model.Dispose();
        _gate.Dispose();
    }

    private static LlmToolCall ParseToolCall(string raw)
    {
        var trimmed = raw.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            throw new LlmOutputException(raw);
        }

        var json = trimmed[start..(end + 1)];

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("name", out var name)
                || name.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(name.GetString())
                || !root.TryGetProperty("arguments", out var arguments)
                || arguments.ValueKind != JsonValueKind.Object)
            {
                throw new LlmOutputException(raw);
            }

            return new LlmToolCall(name.GetString()!, arguments.GetRawText(), raw);
        }
        catch (JsonException)
        {
            throw new LlmOutputException(raw);
        }
    }

    private static string BuildResponseSchema(string toolsSchema)
    {
        using var document = JsonDocument.Parse(toolsSchema);
        var names = document.RootElement
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var schema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["name"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = names,
                },
                ["arguments"] = new Dictionary<string, object?>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = true,
                },
            },
            ["required"] = new[] { "name", "arguments" },
            ["additionalProperties"] = false,
        };

        return JsonSerializer.Serialize(schema);
    }

    private static string Sanitize(string text)
    {
        var sanitized = text;

        foreach (var token in new[]
                 {
                     "<|im_start|>", "<|im_end|>", "<|endoftext|>",
                     "<|system|>", "<|user|>", "<|assistant|>", "<|end|>",
                 })
        {
            sanitized = sanitized.Replace(token, " ", StringComparison.Ordinal);
        }

        return sanitized;
    }

    private static string BuildSystemPrompt(string toolsSchema, DateTimeOffset localNow)
    {
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var tomorrow = today.AddDays(1);
        var dayAfterTomorrow = today.AddDays(2);
        var yesterday = today.AddDays(-1);
        var offset = localNow.Offset;
        var offsetText = (offset < TimeSpan.Zero ? "UTC-" : "UTC+") +
                         offset.Duration().ToString(@"hh\:mm");

        return $$$"""
            Ты — голосовой помощник планировщика. По одной распознанной фразе верни РОВНО ОДИН
            JSON вида {"name":"<tool>","arguments":{...}}. Без markdown и текста вокруг.

            Сейчас: {{{today:yyyy-MM-dd}}} ({{{WeekdayRu(today.DayOfWeek)}}}),
            {{{TimeZoneInfo.Local.Id}}} ({{{offsetText}}}).
            Относительные даты: сегодня={{{today:yyyy-MM-dd}}}, завтра={{{tomorrow:yyyy-MM-dd}}},
            послезавтра={{{dayAfterTomorrow:yyyy-MM-dd}}}, вчера={{{yesterday:yyyy-MM-dd}}}.

            Инструменты:
            {{{toolsSchema}}}

            Правила:
            1. В arguments всегда все ключи; неизвестное значение — null.
            2. Даты только YYYY-MM-DD, время только HH:MM (24 часа).
            3. habit_name и stack_after_title сохраняй как произнёс пользователь, не меняй падеж.
            4. create_habit: recurrence не назван — "daily", «раз в неделю» — "weekly".
            5. log_habit: completed_at = null, если день не назван.
            6. list_habits: "today" для «что на сегодня / что отметить», "all" для «какие у меня
               привычки» и остального.
            7. create_schedule — только разовое событие; повторяющееся действие — create_habit.
            8. Не выдумывай start, end, reward и notes: если не названо — null.
            9. Удаление, изменение и отметка событий не поддерживаются. Если фраза не команда
               из списка или намерение неясно —
               {"name":"unsupported","arguments":{"reason":"<кратко по-русски>"}}.

            Примеры:
            «Отметь планёрку» → {"name":"log_habit","arguments":{"habit_name":"планёрку","completed_at":null}}
            «Я сделал зарядку вчера» → {"name":"log_habit","arguments":{"habit_name":"зарядку","completed_at":"{{{yesterday:yyyy-MM-dd}}}"}}
            «Создай привычку читать каждый день» → {"name":"create_habit","arguments":{"title":"Читать","recurrence":"daily","start":null,"end":null,"reward":null,"stack_after_title":null}}
            «Встреча с Аней завтра в 15:00 на час» → {"name":"create_schedule","arguments":{"title":"Встреча с Аней","date":"{{{tomorrow:yyyy-MM-dd}}}","start":"15:00","end":"16:00","notes":null}}
            «Удали привычку зарядка» → {"name":"unsupported","arguments":{"reason":"удаление не поддерживается"}}
            """;
    }

    private static string WeekdayRu(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "понедельник",
        DayOfWeek.Tuesday => "вторник",
        DayOfWeek.Wednesday => "среда",
        DayOfWeek.Thursday => "четверг",
        DayOfWeek.Friday => "пятница",
        DayOfWeek.Saturday => "суббота",
        _ => "воскресенье",
    };
}
