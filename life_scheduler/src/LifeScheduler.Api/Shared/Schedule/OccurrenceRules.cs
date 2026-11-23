using LifeScheduler.Api.Shared.Data;

namespace LifeScheduler.Api.Shared.Schedule;

/// <summary>Правила вхождений повторяющейся серии — общие для списка и отметок выполнения.</summary>
public static class OccurrenceRules
{
    public static bool IsOccurrence(DateOnly seed, ScheduleRecurrence recurrence, DateOnly date)
    {
        if (recurrence == ScheduleRecurrence.None || date < seed)
            return false;

        return recurrence switch
        {
            ScheduleRecurrence.Daily => true,
            ScheduleRecurrence.Weekly => date.DayOfWeek == seed.DayOfWeek,
            _ => false,
        };
    }

    public static IEnumerable<DateOnly> Dates(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        DateOnly from,
        DateOnly to)
    {
        if (recurrence == ScheduleRecurrence.None)
        {
            if (seed >= from && seed <= to)
                yield return seed;

            yield break;
        }

        if (recurrence == ScheduleRecurrence.Daily)
        {
            for (var date = seed > from ? seed : from; date <= to; date = date.AddDays(1))
                yield return date;

            yield break;
        }

        // Weekly: шагаем от seed, но начинаем с первого вхождения не раньше from.
        var aligned = seed > from ? seed : from;
        var offset = ((int)seed.DayOfWeek - (int)aligned.DayOfWeek + 7) % 7;

        for (var date = aligned.AddDays(offset); date <= to; date = date.AddDays(7))
            yield return date;
    }
}
