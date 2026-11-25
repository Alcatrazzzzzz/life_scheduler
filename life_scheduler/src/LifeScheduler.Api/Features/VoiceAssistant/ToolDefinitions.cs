namespace LifeScheduler.Api.Features.VoiceAssistant;

public static class ToolDefinitions
{
    public const string CreateHabit = "create_habit";
    public const string LogHabit = "log_habit";
    public const string ListHabits = "list_habits";
    public const string CreateSchedule = "create_schedule";
    public const string Unsupported = "unsupported";

    public static readonly string[] Names =
    [
        CreateHabit,
        LogHabit,
        ListHabits,
        CreateSchedule,
    ];

    public const string ToolsJson = """
        [
          {
            "name": "create_habit",
            "description": "Создать новую привычку. Используй для повторяющихся действий: зарядка, чтение, планёрка.",
            "parameters": {
              "type": "object",
              "additionalProperties": false,
              "required": ["title", "recurrence", "start", "end", "reward", "stack_after_title"],
              "properties": {
                "title": { "type": "string", "minLength": 1, "maxLength": 200,
                  "description": "Название привычки в именительном падеже." },
                "recurrence": { "type": "string", "enum": ["daily", "weekly"],
                  "description": "Периодичность: daily — каждый день, weekly — раз в неделю." },
                "start": { "type": ["string", "null"], "pattern": "^([01][0-9]|2[0-3]):[0-5][0-9]$",
                  "description": "Время начала в 24-часовом формате или null." },
                "end": { "type": ["string", "null"], "pattern": "^([01][0-9]|2[0-3]):[0-5][0-9]$",
                  "description": "Время окончания или null. Если start задан, end обязателен." },
                "reward": { "type": ["string", "null"], "maxLength": 200,
                  "description": "Награда после выполнения или null." },
                "stack_after_title": { "type": ["string", "null"],
                  "description": "Название существующей привычки-триггера для stacking или null." }
              }
            }
          },
          {
            "name": "log_habit",
            "description": "Отметить привычку выполненной за конкретный день.",
            "parameters": {
              "type": "object",
              "additionalProperties": false,
              "required": ["habit_name", "completed_at"],
              "properties": {
                "habit_name": { "type": "string", "minLength": 1,
                  "description": "Название привычки так, как его произнёс пользователь." },
                "completed_at": { "type": ["string", "null"], "pattern": "^[0-9]{4}-[0-9]{2}-[0-9]{2}$",
                  "description": "Дата YYYY-MM-DD или null, если день не назван (тогда сегодня)." }
              }
            }
          },
          {
            "name": "list_habits",
            "description": "Показать привычки. Используй для вопросов «что на сегодня», «какие у меня привычки».",
            "parameters": {
              "type": "object",
              "additionalProperties": false,
              "required": ["filter"],
              "properties": {
                "filter": { "type": ["string", "null"], "enum": ["today", "all", null],
                  "description": "today — только запланированные на сегодня; all — все активные; null = all." }
              }
            }
          },
          {
            "name": "create_schedule",
            "description": "Создать разовое событие или встречу в календаре.",
            "parameters": {
              "type": "object",
              "additionalProperties": false,
              "required": ["title", "date", "start", "end", "notes"],
              "properties": {
                "title": { "type": "string", "minLength": 1, "maxLength": 200,
                  "description": "Название события." },
                "date": { "type": "string", "pattern": "^[0-9]{4}-[0-9]{2}-[0-9]{2}$",
                  "description": "Дата YYYY-MM-DD." },
                "start": { "type": ["string", "null"], "pattern": "^([01][0-9]|2[0-3]):[0-5][0-9]$",
                  "description": "Время начала или null." },
                "end": { "type": ["string", "null"], "pattern": "^([01][0-9]|2[0-3]):[0-5][0-9]$",
                  "description": "Время окончания или null." },
                "notes": { "type": ["string", "null"], "maxLength": 2000,
                  "description": "Заметка или null." }
              }
            }
          },
          {
            "name": "unsupported",
            "description": "Безопасный отказ, если команда не относится к поддерживаемым действиям или просит удалить/изменить данные.",
            "parameters": {
              "type": "object",
              "additionalProperties": false,
              "required": ["reason"],
              "properties": {
                "reason": { "type": "string", "minLength": 1,
                  "description": "Короткое объяснение, почему команда не поддерживается." }
              }
            }
          }
        ]
        """;
}
