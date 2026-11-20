using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;

namespace LifeScheduler.Api.Features.Completion;

internal static class CompletionRules
{
    public static Dictionary<string, string[]> Validate(
        ScheduleEntry entry,
        DateOnly? requestedDate,
        DateOnly today,
        out DateOnly date)
    {
        var errors = new Dictionary<string, string[]>();
        date = requestedDate ?? entry.Date;

        if (entry.Kind == ScheduleKind.Event)
        {
            errors["Date"] = ["Отмечать выполнение можно только для задач и привычек."];
            return errors;
        }

        if (entry.Kind == ScheduleKind.Task)
        {
            if (requestedDate is not null && requestedDate != entry.Date)
                errors["Date"] = ["Отметка задачи относится к её дате."];
            else if (entry.Date > today)
                errors["Date"] = ["Нельзя отметить будущую задачу."];

            return errors;
        }

        if (requestedDate is null)
            errors["Date"] = ["Укажите день привычки."];
        else if (requestedDate > today)
            errors["Date"] = ["Нельзя отметить будущий день."];
        else if (!OccurrenceRules.IsOccurrence(entry.Date, entry.Recurrence, requestedDate.Value))
            errors["Date"] = ["В этот день привычка не запланирована."];

        return errors;
    }
}
