using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public static class ListSchedule
{
    public sealed record Response(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeOnly? Start,
        TimeOnly? End,
        string? Notes,
        DateTimeOffset CreatedAt,
        ScheduleRecurrence Recurrence,
        ScheduleKind Kind,
        ScheduleResult Result,
        int AttachmentCount,
        int? Streak);

    public static async Task<Ok<IReadOnlyList<Response>>> Handle(
        DateOnly? from,
        DateOnly? to,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var query = db.Schedules.AsNoTracking()
            // Активные записи и история одного диапазона: прошедшее и отмеченное
            // календарь показывает приглушённым, но не прячет.
            .Where(e => e.Status == ScheduleStatus.Active);

        // Повторяющиеся записи отбираем по дате первого вхождения — дальше они разворачиваются в даты диапазона.
        if (from is not null)
            query = query.Where(e => e.Date >= from.Value || e.Recurrence != ScheduleRecurrence.None);

        if (to is not null)
            query = query.Where(e => e.Date <= to.Value || e.Recurrence != ScheduleRecurrence.None);

        var rows = await query
            .Select(e => new Response(
                e.Id,
                e.Title,
                e.Date,
                e.Start,
                e.End,
                e.Notes,
                e.CreatedAt,
                e.Recurrence,
                e.Kind,
                ScheduleResult.None,
                db.Set<ScheduleAttachment>()
                    .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready),
                null))
            .ToListAsync(ct);

        if (from is null || to is null)
        {
            // Без диапазона повторы не разворачиваем: отдаём каждую запись одним вхождением.
            return TypedResults.Ok<IReadOnlyList<Response>>(Order(rows));
        }

        var completedByHabit = await LoadCompletedOccurrencesAsync(db, rows, ct);
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var currentStreaks = new Dictionary<Guid, int>();

        foreach (var habit in rows.Where(row => row.Kind == ScheduleKind.Habit))
        {
            var completed = completedByHabit.TryGetValue(habit.Id, out var dates)
                ? dates
                : [];
            currentStreaks[habit.Id] =
                HabitStatistics.Streaks(habit.Date, habit.Recurrence, completed, today).Current;
        }

        var activeItems = rows
            .SelectMany(row => OccurrenceRules.Dates(row.Date, row.Recurrence, from.Value, to.Value)
                .Select(date => row with
                {
                    Date = date,
                    Result = row.Kind == ScheduleKind.Habit
                             && IsCompleted(completedByHabit, row.Id, date)
                        ? ScheduleResult.Completed
                        : ScheduleResult.None,
                    Streak = row.Kind == ScheduleKind.Habit
                             && date == today
                             && currentStreaks.TryGetValue(row.Id, out var streak)
                        ? streak
                        : null,
                }));

        var archived = await db.Schedules
            .AsNoTracking()
            .Where(e => e.Status == ScheduleStatus.Archived
                        && e.Date >= from.Value
                        && e.Date <= to.Value)
            .Select(e => new Response(
                e.Id,
                e.Title,
                e.Date,
                e.Start,
                e.End,
                e.Notes,
                e.CreatedAt,
                e.Recurrence,
                e.Kind,
                e.Kind == ScheduleKind.Task
                    ? e.CompletedAt != null ? ScheduleResult.Completed : ScheduleResult.Missed
                    : ScheduleResult.None,
                db.Set<ScheduleAttachment>()
                    .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready),
                null))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<Response>>(Order(activeItems.Concat(archived)));
    }

    private static async Task<Dictionary<Guid, HashSet<DateOnly>>> LoadCompletedOccurrencesAsync(
        LifeSchedulerDbContext db,
        IReadOnlyList<Response> rows,
        CancellationToken ct)
    {
        var habitIds = rows
            .Where(row => row.Kind == ScheduleKind.Habit)
            .Select(row => row.Id)
            .Distinct()
            .ToList();

        if (habitIds.Count == 0)
            return [];

        var completed = await db.Set<ScheduleOccurrenceCompletion>()
            .AsNoTracking()
            .Where(c => habitIds.Contains(c.ScheduleEntryId))
            .Select(c => new { c.ScheduleEntryId, c.Date })
            .ToListAsync(ct);

        return completed
            .GroupBy(c => c.ScheduleEntryId)
            .ToDictionary(group => group.Key, group => group.Select(c => c.Date).ToHashSet());
    }

    private static bool IsCompleted(
        Dictionary<Guid, HashSet<DateOnly>> completed,
        Guid habitId,
        DateOnly date) =>
        completed.TryGetValue(habitId, out var dates) && dates.Contains(date);

    private static List<Response> Order(IEnumerable<Response> items) =>
        items.OrderBy(e => e.Date)
            .ThenBy(e => e.Start.HasValue ? 1 : 0)
            .ThenBy(e => e.Start)
            .ToList();
}
