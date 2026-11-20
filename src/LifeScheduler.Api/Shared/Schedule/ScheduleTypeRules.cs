using LifeScheduler.Api.Shared.Data;

namespace LifeScheduler.Api.Shared.Schedule;

/// <summary>Согласованность типа записи и повторения, а также правила ухода в историю.</summary>
public static class ScheduleTypeRules
{
    public static Dictionary<string, string[]> Validate(
        ScheduleKind kind,
        ScheduleRecurrence recurrence)
    {
        var errors = new Dictionary<string, string[]>();

        if (kind == ScheduleKind.Habit && recurrence == ScheduleRecurrence.None)
            errors["Recurrence"] = ["Для привычки выберите повторение."];

        if (kind == ScheduleKind.Task && recurrence != ScheduleRecurrence.None)
            errors["Recurrence"] = ["Задача не повторяется — для повторений используйте привычку."];

        return errors;
    }

    public static bool IsArchivable(ScheduleKind kind, ScheduleRecurrence recurrence) =>
        kind != ScheduleKind.Habit && recurrence == ScheduleRecurrence.None;

    /// <summary>
    /// Событие уходит в историю сразу после окончания, задача — только после конца дня,
    /// чтобы её можно было отметить до полуночи.
    /// </summary>
    public static bool HasPassed(
        ScheduleKind kind,
        DateOnly date,
        TimeOnly? start,
        TimeOnly? end,
        DateTime localNow) =>
        kind switch
        {
            ScheduleKind.Task => PastEntryRules.IsDayOver(date, localNow),
            ScheduleKind.Habit => false,
            _ => PastEntryRules.IsPast(date, start, end, localNow),
        };
}
