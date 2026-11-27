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
          {"name":"create_habit","description":"Новая привычка — повторяющееся действие.","parameters":{"type":"object","properties":{
            "title":{"type":"string","description":"Название в именительном падеже."},
            "recurrence":{"type":"string","enum":["daily","weekly"],"description":"daily — каждый день, weekly — раз в неделю."},
            "start":{"type":["string","null"],"description":"HH:MM или null."},
            "end":{"type":["string","null"],"description":"HH:MM или null; задаётся вместе со start."},
            "reward":{"type":["string","null"],"description":"Награда или null."},
            "stack_after_title":{"type":["string","null"],"description":"Привычка-триггер для stacking или null."}}}},
          {"name":"log_habit","description":"Отметить привычку выполненной.","parameters":{"type":"object","properties":{
            "habit_name":{"type":"string","description":"Название так, как произнёс пользователь."},
            "completed_at":{"type":["string","null"],"description":"YYYY-MM-DD или null (сегодня)."}}}},
          {"name":"list_habits","description":"Показать привычки.","parameters":{"type":"object","properties":{
            "filter":{"type":["string","null"],"enum":["today","all",null],"description":"today — на сегодня, all — все активные."}}}},
          {"name":"create_schedule","description":"Разовое событие или встреча.","parameters":{"type":"object","properties":{
            "title":{"type":"string","description":"Название события."},
            "date":{"type":"string","description":"YYYY-MM-DD."},
            "start":{"type":["string","null"],"description":"HH:MM или null."},
            "end":{"type":["string","null"],"description":"HH:MM или null."},
            "notes":{"type":["string","null"],"description":"Заметка или null."}}}},
          {"name":"unsupported","description":"Отказ: команда не поддерживается.","parameters":{"type":"object","properties":{
            "reason":{"type":"string","description":"Краткое объяснение."}}}}
        ]
        """;
}
