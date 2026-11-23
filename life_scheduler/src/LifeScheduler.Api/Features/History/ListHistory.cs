using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.History;

public enum HistoryResult
{
    None,
    Completed,
    Missed,
}

public static class ListHistory
{
    private const int MaxItems = 200;

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
        HistoryResult Result,
        int AttachmentCount);

    public static async Task<Ok<IReadOnlyList<Response>>> Handle(
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        // События и задачи живут строками в Schedules, отметки привычек — отдельными днями.
        var entries = await db.Schedules
            .AsNoTracking()
            .Where(e => e.Status == ScheduleStatus.Archived
                        && e.Recurrence == ScheduleRecurrence.None
                        && e.Kind != ScheduleKind.Habit)
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
                    ? e.CompletedAt != null ? HistoryResult.Completed : HistoryResult.Missed
                    : HistoryResult.None,
                db.Set<ScheduleAttachment>()
                    .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready)))
            .ToListAsync(ct);

        var habitMarks = await db.Set<ScheduleOccurrenceCompletion>()
            .AsNoTracking()
            .Join(
                db.Schedules.AsNoTracking().Where(e => e.Kind == ScheduleKind.Habit),
                c => c.ScheduleEntryId,
                e => e.Id,
                (c, e) => new Response(
                    e.Id,
                    e.Title,
                    c.Date,
                    e.Start,
                    e.End,
                    e.Notes,
                    e.CreatedAt,
                    e.Recurrence,
                    e.Kind,
                    HistoryResult.Completed,
                    db.Set<ScheduleAttachment>()
                        .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready)))
            .ToListAsync(ct);

        var items = entries
            .Concat(habitMarks)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Start)
            .Take(MaxItems)
            .ToList();

        return TypedResults.Ok<IReadOnlyList<Response>>(items);
    }
}
