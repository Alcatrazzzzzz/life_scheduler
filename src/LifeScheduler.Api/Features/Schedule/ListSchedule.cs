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
        int AttachmentCount);

    public static async Task<Ok<IReadOnlyList<Response>>> Handle(
        DateOnly? from,
        DateOnly? to,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var query = db.Schedules.AsNoTracking()
            // Календарь показывает только актуальное: выполненное и прошедшее живёт в истории.
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
                db.Set<ScheduleAttachment>()
                    .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready)))
            .ToListAsync(ct);

        if (from is null || to is null)
        {
            // Без диапазона повторы не разворачиваем: отдаём каждую запись одним вхождением.
            return TypedResults.Ok<IReadOnlyList<Response>>(Order(rows));
        }

        var completedByHabit = await LoadCompletedOccurrencesAsync(db, rows, ct);

        var items = rows
            .SelectMany(row => OccurrenceRules.Dates(row.Date, row.Recurrence, from.Value, to.Value)
                .Where(date => !completedByHabit.Contains((row.Id, date)))
                .Select(date => row with { Date = date }))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<Response>>(Order(items));
    }

    private static async Task<HashSet<(Guid Id, DateOnly Date)>> LoadCompletedOccurrencesAsync(
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

        return completed.Select(c => (c.ScheduleEntryId, c.Date)).ToHashSet();
    }

    private static List<Response> Order(IEnumerable<Response> items) =>
        items.OrderBy(e => e.Date)
            .ThenBy(e => e.Start.HasValue ? 1 : 0)
            .ThenBy(e => e.Start)
            .ToList();
}
