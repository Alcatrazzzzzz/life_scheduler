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
    private readonly SemaphoreSlim _gate = new(1, 1);

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
            var systemPrompt = BuildSystemPrompt(toolsSchema, _time.GetLocalNow());
            var responseSchema = BuildResponseSchema(toolsSchema);
            var messages = JsonSerializer.Serialize(new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = Sanitize(userText) },
            });

            var prompt = _tokenizer.ApplyChatTemplate(
                string.Empty,
                messages,
                string.Empty,
                true);

            using var parameters = new GeneratorParams(_model);
            parameters.SetSearchOption("max_length", Math.Max(1024, _options.OnnxMaxLength));
            parameters.SetGuidance("json_schema", responseSchema);

            using var generator = new Generator(_model, parameters);
            using var sequences = _tokenizer.Encode(prompt);
            generator.AppendTokenSequences(sequences);
            using var stream = _tokenizer.CreateStream();

            var raw = new StringBuilder();

            while (!generator.IsDone())
            {
                generator.GenerateNextToken();

                if (generator.IsDone())
                {
                    break;
                }

                raw.Append(stream.Decode(generator.GetNextTokens()[0]));
            }

            return ParseToolCall(raw.ToString());
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
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
        var time = TimeOnly.FromDateTime(localNow.DateTime).ToString("HH:mm");
        var offset = localNow.Offset;
        var offsetText = (offset < TimeSpan.Zero ? "UTC-" : "UTC+") +
                         offset.Duration().ToString(@"hh\:mm");

        return $$$"""
            Ты — локальный голосовой помощник планировщика задач. Ты получаешь одну фразу,
            распознанную из речи, и обязан вернуть РОВНО ОДИН JSON-объект и ничего больше.

            Текущая дата: {{{today:yyyy-MM-dd}}} ({{{WeekdayRu(today.DayOfWeek)}}}).
            Текущее локальное время: {{{time}}}.
            Часовой пояс: {{{TimeZoneInfo.Local.Id}}} ({{{offsetText}}}).

            Относительные даты считай от текущей даты:
            «сегодня» = {{{today:yyyy-MM-dd}}}, «завтра» = {{{tomorrow:yyyy-MM-dd}}},
            «послезавтра» = {{{dayAfterTomorrow:yyyy-MM-dd}}}, «вчера» = {{{yesterday:yyyy-MM-dd}}}.

            Доступные инструменты (JSON-схемы):
            {{{toolsSchema}}}

            Правила:
            1. Ответ — только JSON вида {"name": "<tool>", "arguments": {...}}. Без markdown,
               без пояснений, без текста до или после JSON.
            2. Все ключи arguments обязательны. Если значение не названо — null.
            3. Даты — только YYYY-MM-DD. Время — только HH:MM (24 часа).
            4. Для habit_name и stack_after_title сохраняй название так, как его произнёс
               пользователь. Не придумывай именительный падеж и не переводи.
            5. create_habit: recurrence обязателен. Если периодичность не названа — "daily".
               Для привычки «каждый день» — daily, «раз в неделю» — weekly.
            6. log_habit: completed_at = null, если день не назван (помощник подставит сегодня).
            7. list_habits: filter = "today" для «что на сегодня / что отметить»; иначе "all".
            8. create_schedule: только разовое событие или встреча. Повторяющееся действие —
               это create_habit.
            9. Никогда не вызывай удаление, изменение или отметку события: таких инструментов нет.
            10. Если фраза не является командой из списка, просит удалить/изменить данные или
                ты не уверен в намерении — верни
                {"name":"unsupported","arguments":{"reason":"<кратко по-русски>"}}.

            Примеры:
            Пользователь: «Отметь планёрку»
            Ответ: {"name":"log_habit","arguments":{"habit_name":"планёрку","completed_at":null}}

            Пользователь: «Я сделал зарядку вчера»
            Ответ: {"name":"log_habit","arguments":{"habit_name":"зарядку","completed_at":"{{{yesterday:yyyy-MM-dd}}}"}}

            Пользователь: «Создай привычку читать каждый день»
            Ответ: {"name":"create_habit","arguments":{"title":"Читать","recurrence":"daily","start":null,"end":null,"reward":null,"stack_after_title":null}}

            Пользователь: «Что у меня на сегодня из привычек»
            Ответ: {"name":"list_habits","arguments":{"filter":"today"}}

            Пользователь: «Встреча с Аней завтра в 15:00 на час»
            Ответ: {"name":"create_schedule","arguments":{"title":"Встреча с Аней","date":"{{{tomorrow:yyyy-MM-dd}}}","start":"15:00","end":"16:00","notes":null}}

            Пользователь: «Удали привычку зарядка»
            Ответ: {"name":"unsupported","arguments":{"reason":"удаление не поддерживается"}}
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
