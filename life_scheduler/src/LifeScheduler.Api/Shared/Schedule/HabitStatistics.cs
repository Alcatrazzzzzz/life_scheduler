using LifeScheduler.Api.Shared.Data;

namespace LifeScheduler.Api.Shared.Schedule;

public readonly record struct HabitDay(DateOnly Date, bool Scheduled, bool Completed);

public readonly record struct HabitStreaks(int Current, int Longest);

public readonly record struct HabitMisses(
    bool LastOccurrenceMissed,
    bool MissedTwice,
    DateOnly? LastMissedDate);

/// <summary>Серии и пропуски привычки по её seed, повторению и отметкам дней.</summary>
public static class HabitStatistics
{
    public static int StepDays(ScheduleRecurrence recurrence) =>
        recurrence == ScheduleRecurrence.Weekly ? 7 : 1;

    /// <summary>Последнее вхождение серии не позже указанной даты.</summary>
    public static DateOnly? LastOccurrenceOnOrBefore(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        DateOnly date)
    {
        if (recurrence == ScheduleRecurrence.None || date < seed)
            return null;

        if (recurrence == ScheduleRecurrence.Daily)
            return date;

        var aligned = date.AddDays(-(((int)date.DayOfWeek - (int)seed.DayOfWeek + 7) % 7));
        return aligned < seed ? null : aligned;
    }

    /// <summary>Первое вхождение серии не раньше указанной даты.</summary>
    public static DateOnly? NextOccurrence(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        DateOnly from)
    {
        if (recurrence == ScheduleRecurrence.None)
            return seed >= from ? seed : null;

        if (recurrence == ScheduleRecurrence.Daily)
            return seed > from ? seed : from;

        var aligned = seed > from ? seed : from;
        var offset = ((int)seed.DayOfWeek - (int)aligned.DayOfWeek + 7) % 7;
        return aligned.AddDays(offset);
    }

    /// <summary>
    /// Текущая и лучшая серии. Незакрытое «сегодня» текущую серию не рвёт:
    /// отсчёт идёт от предыдущего вхождения.
    /// </summary>
    public static HabitStreaks Streaks(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        IReadOnlySet<DateOnly> completed,
        DateOnly today)
    {
        if (recurrence == ScheduleRecurrence.None)
            return new HabitStreaks(0, 0);

        var step = StepDays(recurrence);
        var last = LastOccurrenceOnOrBefore(seed, recurrence, today);
        var current = 0;

        if (last is { } lastDate)
        {
            // Пропущенное прошедшее вхождение обрывает серию; незакрытое сегодня — нет.
            var counted = completed.Contains(lastDate) || lastDate == today;
            var cursor = completed.Contains(lastDate)
                ? lastDate
                : lastDate.AddDays(-step);

            while (counted && cursor >= seed && completed.Contains(cursor))
            {
                current += 1;
                cursor = cursor.AddDays(-step);
            }
        }

        var longest = 0;
        var run = 0;
        DateOnly? previous = null;

        foreach (var date in completed
                     .Where(d => d >= seed
                                 && d <= today
                                 && OccurrenceRules.IsOccurrence(seed, recurrence, d))
                     .Order())
        {
            run = previous is { } previousDate && previousDate.AddDays(step) == date ? run + 1 : 1;
            previous = date;

            if (run > longest)
                longest = run;
        }

        return new HabitStreaks(current, longest);
    }

    /// <summary>Пропуски среди уже прошедших вхождений (сегодня ещё не считается пропуском).</summary>
    public static HabitMisses FindMisses(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        IReadOnlySet<DateOnly> completed,
        DateOnly today)
    {
        var lastDue = LastOccurrenceOnOrBefore(seed, recurrence, today.AddDays(-1));
        if (lastDue is not { } lastDate)
            return new HabitMisses(false, false, null);

        var lastMissed = !completed.Contains(lastDate);
        if (!lastMissed)
            return new HabitMisses(false, false, null);

        var step = StepDays(recurrence);
        var previous = lastDate.AddDays(-step);
        var missedTwice = previous >= seed && !completed.Contains(previous);

        return new HabitMisses(true, missedTwice, lastDate);
    }

    public static IReadOnlyList<HabitDay> Window(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        IReadOnlySet<DateOnly> completed,
        DateOnly from,
        DateOnly to)
    {
        var days = new List<HabitDay>();
        if (to < from)
            return days;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var scheduled = OccurrenceRules.IsOccurrence(seed, recurrence, date);
            days.Add(new HabitDay(date, scheduled, scheduled && completed.Contains(date)));
        }

        return days;
    }

    /// <summary>Доля отмеченных среди прошедших вхождений окна; null, если прошедших нет.</summary>
    public static double? CompletionRate(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        IReadOnlySet<DateOnly> completed,
        DateOnly from,
        DateOnly to,
        DateOnly today)
    {
        var lastDue = to < today.AddDays(-1) ? to : today.AddDays(-1);
        if (lastDue < from)
            return null;

        var due = 0;
        var done = 0;

        for (var date = from; date <= lastDue; date = date.AddDays(1))
        {
            if (!OccurrenceRules.IsOccurrence(seed, recurrence, date))
                continue;

            due += 1;
            if (completed.Contains(date))
                done += 1;
        }

        return due == 0 ? null : (double)done / due;
    }
}
