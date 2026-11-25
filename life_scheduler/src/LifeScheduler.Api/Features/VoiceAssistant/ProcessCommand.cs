using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.LocalAi;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeScheduler.Api.Features.VoiceAssistant;

public static class ProcessCommand
{
    public const int MaxTextLength = 1000;

    private const string CorrectiveSuffix =
        "\n\nВАЖНО: предыдущий ответ не соответствовал схеме. Верни ровно один JSON " +
        "{\"name\":\"...\",\"arguments\":{...}} с инструментом create_habit, log_habit, " +
        "list_habits или create_schedule.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public sealed record TextRequest(string Text);

    public sealed record ActionResponse(string Name, JsonElement Arguments);

    public sealed record ResultPayload(
        string Kind,
        string Message,
        Guid? Id = null,
        string? Title = null,
        DateOnly? Date = null,
        bool? AlreadyLogged = null,
        IReadOnlyList<string>? Habits = null);

    public sealed record Response(
        string RecognizedText,
        ActionResponse Action,
        ResultPayload Result);

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/command", HandleAsync)
             .WithName("ProcessVoiceCommand")
             .Produces<Response>()
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
             .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
             .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
             .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    private static async Task<Results<Ok<Response>, ValidationProblem, ProblemHttpResult>> HandleAsync(
        HttpRequest http,
        IWhisperTranscriber whisper,
        ILlmFunctionCaller llm,
        LifeSchedulerDbContext db,
        TimeProvider time,
        IOptions<LocalAiOptions> options,
        CancellationToken ct)
    {
        try
        {
            var input = await ReadInputAsync(http, whisper, options.Value, ct);

            if (input.Validation is not null)
            {
                return input.Validation;
            }

            if (input.Problem is not null)
            {
                return input.Problem;
            }

            return await ExecuteAsync(input.Text!, llm, db, time, ct);
        }
        catch (LocalAiUnavailableException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Ассистент отключён",
                detail: "Локальный AI выключен через LocalAi:Enabled=false. См. README.md.");
        }
        catch (AudioDecodeException ex)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Не удалось обработать аудио",
                detail: ex.Message);
        }
    }

    private static async Task<Results<Ok<Response>, ValidationProblem, ProblemHttpResult>> ExecuteAsync(
        string text,
        ILlmFunctionCaller llm,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        LlmToolCall? lastCall = null;
        ExecutionOutcome outcome;

        try
        {
            lastCall = await llm.CallAsync(text, ToolDefinitions.ToolsJson, ct);
            outcome = await ExecuteCallAsync(lastCall, db, time, ct);
        }
        catch (LlmOutputException ex)
        {
            outcome = new ExecutionOutcome.Retryable(ex.RawOutput);
        }

        if (outcome is ExecutionOutcome.Retryable)
        {
            try
            {
                lastCall = await llm.CallAsync(text + CorrectiveSuffix, ToolDefinitions.ToolsJson, ct);
                outcome = await ExecuteCallAsync(lastCall, db, time, ct);
            }
            catch (LlmOutputException ex)
            {
                outcome = new ExecutionOutcome.Retryable(ex.RawOutput);
            }
        }

        return outcome switch
        {
            ExecutionOutcome.Success success when lastCall is not null =>
                TypedResults.Ok(new Response(text, BuildAction(lastCall), success.Payload)),
            ExecutionOutcome.Failure failure => failure.Problem,
            ExecutionOutcome.Retryable retryable => InvalidLlm(retryable.RawOutput),
            _ => InvalidLlm(string.Empty),
        };
    }

    private static async Task<ExecutionOutcome> ExecuteCallAsync(
        LlmToolCall call,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (string.Equals(call.Name, ToolDefinitions.Unsupported, StringComparison.Ordinal))
        {
            return new ExecutionOutcome.Failure(
                Unprocessable("Команда не поддерживается", ReadReason(call.ArgumentsJson)));
        }

        if (!ToolDefinitions.Names.Contains(call.Name, StringComparer.Ordinal))
        {
            return new ExecutionOutcome.Retryable(call.RawOutput);
        }

        switch (call.Name)
        {
            case ToolDefinitions.CreateHabit:
                {
                    var args = Deserialize<CreateHabitArgs>(call.ArgumentsJson);
                    return args is null
                        ? new ExecutionOutcome.Retryable(call.RawOutput)
                        : await CreateHabitAsync(args, db, time, ct);
                }

            case ToolDefinitions.LogHabit:
                {
                    var args = Deserialize<LogHabitArgs>(call.ArgumentsJson);
                    return args is null
                        ? new ExecutionOutcome.Retryable(call.RawOutput)
                        : await LogHabitAsync(args, db, time, ct);
                }

            case ToolDefinitions.ListHabits:
                {
                    var args = Deserialize<ListHabitsArgs>(call.ArgumentsJson);
                    return args is null
                        ? new ExecutionOutcome.Retryable(call.RawOutput)
                        : await ListHabitsAsync(args, db, time, ct);
                }

            case ToolDefinitions.CreateSchedule:
                {
                    var args = Deserialize<CreateScheduleArgs>(call.ArgumentsJson);
                    return args is null
                        ? new ExecutionOutcome.Retryable(call.RawOutput)
                        : await CreateScheduleAsync(args, db, time, ct);
                }

            default:
                return new ExecutionOutcome.Retryable(call.RawOutput);
        }
    }

    private static async Task<ExecutionOutcome> CreateHabitAsync(
        CreateHabitArgs args,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var title = args.Title?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            return new ExecutionOutcome.Retryable("Отсутствует title.");
        }

        if (title.Length > 200)
        {
            return UnprocessableResult("Название привычки", "Название не длиннее 200 символов.");
        }

        if (!TryParseRecurrence(args.Recurrence, out var recurrence))
        {
            return new ExecutionOutcome.Retryable("Некорректное recurrence.");
        }

        if (!TryParseTime(args.Start, out var start) || !TryParseTime(args.End, out var end))
        {
            return new ExecutionOutcome.Retryable("Некорректное время.");
        }

        if ((start is null) != (end is null))
        {
            return new ExecutionOutcome.Retryable("start и end задаются только вместе.");
        }

        if (start is { } startValue && end is { } endValue && endValue <= startValue)
        {
            return new ExecutionOutcome.Retryable("end должен быть позже start.");
        }

        var reward = args.Reward?.Trim();

        if (reward?.Length > HabitRules.MaxRewardLength)
        {
            return UnprocessableResult(
                "Награда",
                $"Награда не длиннее {HabitRules.MaxRewardLength} символов.");
        }

        Guid? stackAfterId = null;

        if (!string.IsNullOrWhiteSpace(args.StackAfterTitle))
        {
            var candidates = await LoadHabitCandidatesAsync(db, ct);
            var lookup = HabitNameMatcher.Lookup(args.StackAfterTitle, candidates);

            if (lookup.Ambiguous)
            {
                return new ExecutionOutcome.Failure(AmbiguousProblem(lookup));
            }

            if (lookup.HabitId is null)
            {
                return new ExecutionOutcome.Failure(
                    NotFoundProblem(args.StackAfterTitle, lookup));
            }

            stackAfterId = lookup.HabitId;
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var now = time.GetUtcNow();

        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            Title = title,
            Kind = ScheduleKind.Habit,
            Date = today,
            Start = start,
            End = end,
            Notes = null,
            Reward = HabitRules.NormalizeReward(reward),
            StackAfterId = stackAfterId,
            Recurrence = recurrence,
            Status = ScheduleStatus.Active,
            ArchivedAt = null,
            CreatedAt = now,
        };

        db.Schedules.Add(entry);
        await db.SaveChangesAsync(ct);

        var scheduleText = recurrence == ScheduleRecurrence.Daily
            ? "каждый день"
            : "раз в неделю";

        return new ExecutionOutcome.Success(new ResultPayload(
            "habit_created",
            $"Привычка «{entry.Title}» создана ({scheduleText}).",
            entry.Id,
            entry.Title,
            entry.Date));
    }

    private static async Task<ExecutionOutcome> LogHabitAsync(
        LogHabitArgs args,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(args.HabitName))
        {
            return new ExecutionOutcome.Retryable("Отсутствует habit_name.");
        }

        var candidates = await LoadHabitCandidatesAsync(db, ct);
        var lookup = HabitNameMatcher.Lookup(args.HabitName, candidates);

        if (lookup.Ambiguous)
        {
            return new ExecutionOutcome.Failure(AmbiguousProblem(lookup));
        }

        if (lookup.HabitId is null)
        {
            return new ExecutionOutcome.Failure(NotFoundProblem(args.HabitName, lookup));
        }

        var habitId = lookup.HabitId.Value;
        var habit = await db.Schedules.FirstOrDefaultAsync(e => e.Id == habitId, ct);

        if (habit is null)
        {
            return new ExecutionOutcome.Failure(NotFoundProblem(args.HabitName, lookup));
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var date = today;

        if (!string.IsNullOrWhiteSpace(args.CompletedAt)
            && !TryParseDate(args.CompletedAt, out date))
        {
            return new ExecutionOutcome.Retryable("Некорректная дата completed_at.");
        }

        if (date > today)
        {
            return UnprocessableResult("Дата отметки", "Нельзя отметить будущий день.");
        }

        if (!OccurrenceRules.IsOccurrence(habit.Date, habit.Recurrence, date))
        {
            return UnprocessableResult(
                "Дата отметки",
                "В этот день привычка не запланирована.");
        }

        var alreadyLogged = await db.OccurrenceCompletions
            .AnyAsync(c => c.ScheduleEntryId == habitId && c.Date == date, ct);

        if (!alreadyLogged)
        {
            db.OccurrenceCompletions.Add(new ScheduleOccurrenceCompletion
            {
                Id = Guid.NewGuid(),
                ScheduleEntryId = habitId,
                Date = date,
                CompletedAt = time.GetUtcNow(),
            });

            await db.SaveChangesAsync(ct);
        }

        var dateText = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var message = alreadyLogged
            ? $"Привычка «{habit.Title}» уже отмечена за {dateText}."
            : $"Привычка «{habit.Title}» отмечена за {dateText}.";

        return new ExecutionOutcome.Success(new ResultPayload(
            "habit_logged",
            message,
            habit.Id,
            habit.Title,
            date,
            alreadyLogged));
    }

    private static async Task<ExecutionOutcome> ListHabitsAsync(
        ListHabitsArgs args,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        bool todayOnly;

        if (string.IsNullOrWhiteSpace(args.Filter)
            || string.Equals(args.Filter, "all", StringComparison.OrdinalIgnoreCase))
        {
            todayOnly = false;
        }
        else if (string.Equals(args.Filter, "today", StringComparison.OrdinalIgnoreCase))
        {
            todayOnly = true;
        }
        else
        {
            return new ExecutionOutcome.Retryable("Некорректный filter.");
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

        var habits = await db.Schedules.AsNoTracking()
            .Where(e => e.Kind == ScheduleKind.Habit && e.Status == ScheduleStatus.Active)
            .OrderBy(e => e.Title)
            .Select(e => new HabitRow(e.Id, e.Title, e.Recurrence, e.Date))
            .ToListAsync(ct);

        if (todayOnly)
        {
            habits = habits
                .Where(h => OccurrenceRules.IsOccurrence(h.Date, h.Recurrence, today))
                .ToList();
        }

        var ids = habits.Select(h => h.Id).ToList();
        List<Guid> done;

        if (ids.Count == 0)
        {
            done = [];
        }
        else
        {
            done = await db.OccurrenceCompletions.AsNoTracking()
                .Where(c => c.Date == today && ids.Contains(c.ScheduleEntryId))
                .Select(c => c.ScheduleEntryId)
                .ToListAsync(ct);
        }

        var lines = habits
            .Select(h => $"{h.Title} — {(done.Contains(h.Id) ? "выполнено" : "не отмечено")}")
            .ToList();

        var message = habits.Count == 0
            ? todayOnly ? "На сегодня привычек нет." : "Активных привычек нет."
            : (todayOnly ? "Привычки на сегодня: " : "Активные привычки: ")
              + string.Join("; ", lines);

        return new ExecutionOutcome.Success(new ResultPayload(
            "habits_listed",
            message,
            Habits: lines));
    }

    private static async Task<ExecutionOutcome> CreateScheduleAsync(
        CreateScheduleArgs args,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var title = args.Title?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            return new ExecutionOutcome.Retryable("Отсутствует title.");
        }

        if (title.Length > 200)
        {
            return UnprocessableResult("Название события", "Название не длиннее 200 символов.");
        }

        if (!TryParseDate(args.Date, out var date))
        {
            return new ExecutionOutcome.Retryable("Некорректная дата.");
        }

        if (!TryParseTime(args.Start, out var start) || !TryParseTime(args.End, out var end))
        {
            return new ExecutionOutcome.Retryable("Некорректное время.");
        }

        if ((start is null) != (end is null))
        {
            return new ExecutionOutcome.Retryable("start и end задаются только вместе.");
        }

        if (start is { } startValue && end is { } endValue && endValue <= startValue)
        {
            return new ExecutionOutcome.Retryable("end должен быть позже start.");
        }

        var notes = args.Notes?.Trim();

        if (notes?.Length > 2000)
        {
            return UnprocessableResult("Заметка", "Заметка не длиннее 2000 символов.");
        }

        var localNow = time.GetLocalNow().DateTime;
        var isPast = ScheduleTypeRules.IsArchivable(ScheduleKind.Event, ScheduleRecurrence.None)
                     && ScheduleTypeRules.HasPassed(
                         ScheduleKind.Event, date, start, end, localNow);
        var now = time.GetUtcNow();

        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            Title = title,
            Kind = ScheduleKind.Event,
            Date = date,
            Start = start,
            End = end,
            Notes = notes,
            Reward = null,
            StackAfterId = null,
            Recurrence = ScheduleRecurrence.None,
            Status = isPast ? ScheduleStatus.Archived : ScheduleStatus.Active,
            ArchivedAt = isPast ? now : null,
            CreatedAt = now,
        };

        db.Schedules.Add(entry);
        await db.SaveChangesAsync(ct);

        var dateText = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var timeText = start is { } startTime && end is { } endTime
            ? $", {startTime.ToString("HH:mm", CultureInfo.InvariantCulture)}–" +
              endTime.ToString("HH:mm", CultureInfo.InvariantCulture)
            : string.Empty;

        return new ExecutionOutcome.Success(new ResultPayload(
            "schedule_created",
            $"Событие «{entry.Title}» создано на {dateText}{timeText}.",
            entry.Id,
            entry.Title,
            entry.Date));
    }

    private static async Task<InputResult> ReadInputAsync(
        HttpRequest request,
        IWhisperTranscriber whisper,
        LocalAiOptions options,
        CancellationToken ct)
    {
        if (request.HasJsonContentType())
        {
            if (request.ContentLength is > 64 * 1024)
            {
                return InputResult.FromProblem(TooLarge("Текстовый запрос слишком большой."));
            }

            TextRequest? body;

            try
            {
                body = await request.ReadFromJsonAsync<TextRequest>(JsonOptions, ct);
            }
            catch (JsonException)
            {
                return InputResult.FromValidation(Validation("text", "Некорректный JSON."));
            }

            if (body is null || string.IsNullOrWhiteSpace(body.Text))
            {
                return InputResult.FromValidation(Validation("text", "Текст команды обязателен."));
            }

            var text = body.Text.Trim();

            if (text.Length > MaxTextLength)
            {
                return InputResult.FromProblem(TooLarge("Команда слишком длинная."));
            }

            return InputResult.Success(text);
        }

        if (request.HasFormContentType)
        {
            var maxRequestBytes = options.MaxAudioBytes + (1024 * 1024);
            var sizeFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

            if (sizeFeature is { IsReadOnly: false })
            {
                sizeFeature.MaxRequestBodySize = maxRequestBytes;
            }

            if (request.ContentLength > maxRequestBytes)
            {
                return InputResult.FromProblem(TooLarge("Аудиофайл слишком большой."));
            }

            IFormCollection form;

            try
            {
                form = await request.ReadFormAsync(ct);
            }
            catch (InvalidDataException)
            {
                return InputResult.FromValidation(
                    Validation("audio", "Некорректный multipart-запрос."));
            }

            var file = form.Files.GetFile("audio")
                       ?? (form.Files.Count == 1 ? form.Files[0] : null);

            if (file is null)
            {
                return InputResult.FromValidation(
                    Validation("audio", "Добавьте аудиофайл в поле audio."));
            }

            if (file.Length > options.MaxAudioBytes)
            {
                return InputResult.FromProblem(TooLarge("Аудиофайл больше 10 МБ."));
            }

            var mediaType = AudioDecoder.NormalizeMediaType(file.ContentType);

            if (!IsSupportedAudio(mediaType))
            {
                return InputResult.FromProblem(TypedResults.Problem(
                    statusCode: StatusCodes.Status415UnsupportedMediaType,
                    title: "Файл не является аудио",
                    detail: "Поддерживаются wav, webm/opus и ogg/opus."));
            }

            await using var buffer = new MemoryStream((int)Math.Min(file.Length, options.MaxAudioBytes));
            await file.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var recognized = await whisper.TranscribeAsync(
                buffer, mediaType, file.FileName, ct);

            if (string.IsNullOrWhiteSpace(recognized))
            {
                return InputResult.FromProblem(TypedResults.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Не удалось распознать речь",
                    detail: "Попробуйте сказать ещё раз."));
            }

            return InputResult.Success(recognized.Trim());
        }

        return InputResult.FromProblem(TypedResults.Problem(
            statusCode: StatusCodes.Status415UnsupportedMediaType,
            title: "Неподдерживаемый тип запроса",
            detail: "Передайте application/json с полем text или multipart/form-data с полем audio."));
    }

    private static Task<List<HabitCandidate>> LoadHabitCandidatesAsync(
        LifeSchedulerDbContext db,
        CancellationToken ct) =>
        db.Schedules.AsNoTracking()
            .Where(e => e.Kind == ScheduleKind.Habit && e.Status == ScheduleStatus.Active)
            .Select(e => new HabitCandidate(e.Id, e.Title))
            .ToListAsync(ct);

    private static ActionResponse BuildAction(LlmToolCall call)
    {
        try
        {
            using var document = JsonDocument.Parse(call.ArgumentsJson);
            return new ActionResponse(call.Name, document.RootElement.Clone());
        }
        catch (JsonException)
        {
            using var empty = JsonDocument.Parse("{}");
            return new ActionResponse(call.Name, empty.RootElement.Clone());
        }
    }

    private static T? Deserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadReason(string argumentsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(argumentsJson);

            return document.RootElement.TryGetProperty("reason", out var reason)
                   && reason.ValueKind == JsonValueKind.String
                ? reason.GetString() ?? "команда не поддерживается"
                : "команда не поддерживается";
        }
        catch (JsonException)
        {
            return "команда не поддерживается";
        }
    }

    private static bool TryParseTime(string? value, out TimeOnly? time)
    {
        time = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (TimeOnly.TryParseExact(
                value.Trim(),
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            time = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(
            value?.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static bool TryParseRecurrence(
        string? value,
        out ScheduleRecurrence recurrence)
    {
        recurrence = ScheduleRecurrence.Daily;

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "daily", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(value, "weekly", StringComparison.OrdinalIgnoreCase))
        {
            recurrence = ScheduleRecurrence.Weekly;
            return true;
        }

        return false;
    }

    private static bool IsSupportedAudio(string? mediaType) =>
        mediaType is null
        || mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
        || mediaType is "application/octet-stream" or "video/webm";

    private static ValidationProblem Validation(string key, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [key] = [message],
        });

    private static ProblemHttpResult TooLarge(string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status413PayloadTooLarge,
            title: "Слишком большой запрос",
            detail: detail);

    private static ProblemHttpResult Unprocessable(string title, string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: title,
            detail: detail);

    private static ExecutionOutcome UnprocessableResult(string title, string detail) =>
        new ExecutionOutcome.Failure(Unprocessable(title, detail));

    private static ProblemHttpResult InvalidLlm(string rawOutput) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Команда не распознана",
            detail: string.IsNullOrWhiteSpace(rawOutput)
                ? "LLM не вернула вызов инструмента."
                : rawOutput);

    private static ProblemHttpResult NotFoundProblem(
        string spoken,
        HabitLookupResult lookup)
    {
        var detail = lookup.Suggestions.Count > 0
            ? $"Не нашёл «{spoken}». Ближайшие: {string.Join(", ", lookup.Suggestions)}."
            : $"Не нашёл привычку «{spoken}».";

        return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Привычка не найдена",
            detail: detail);
    }

    private static ProblemHttpResult AmbiguousProblem(HabitLookupResult lookup) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Уточните привычку",
            detail: $"Похожие привычки: {string.Join(", ", lookup.Suggestions)}.");

    private readonly record struct InputResult(
        string? Text,
        ValidationProblem? Validation,
        ProblemHttpResult? Problem)
    {
        public static InputResult Success(string text) => new(text, null, null);

        public static InputResult FromValidation(ValidationProblem validation) =>
            new(null, validation, null);

        public static InputResult FromProblem(ProblemHttpResult problem) =>
            new(null, null, problem);
    }

    private abstract record ExecutionOutcome
    {
        public sealed record Success(ResultPayload Payload) : ExecutionOutcome;

        public sealed record Retryable(string RawOutput) : ExecutionOutcome;

        public sealed record Failure(ProblemHttpResult Problem) : ExecutionOutcome;
    }

    private sealed record CreateHabitArgs
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("recurrence")]
        public string? Recurrence { get; init; }

        [JsonPropertyName("start")]
        public string? Start { get; init; }

        [JsonPropertyName("end")]
        public string? End { get; init; }

        [JsonPropertyName("reward")]
        public string? Reward { get; init; }

        [JsonPropertyName("stack_after_title")]
        public string? StackAfterTitle { get; init; }
    }

    private sealed record LogHabitArgs
    {
        [JsonPropertyName("habit_name")]
        public string? HabitName { get; init; }

        [JsonPropertyName("completed_at")]
        public string? CompletedAt { get; init; }
    }

    private sealed record ListHabitsArgs
    {
        [JsonPropertyName("filter")]
        public string? Filter { get; init; }
    }

    private sealed record CreateScheduleArgs
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("start")]
        public string? Start { get; init; }

        [JsonPropertyName("end")]
        public string? End { get; init; }

        [JsonPropertyName("notes")]
        public string? Notes { get; init; }
    }

    private sealed record HabitRow(
        Guid Id,
        string Title,
        ScheduleRecurrence Recurrence,
        DateOnly Date);
}
