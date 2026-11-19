using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.History;

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
        int AttachmentCount);

    public static async Task<Ok<IReadOnlyList<Response>>> Handle(
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var items = await db.Schedules
            .AsNoTracking()
            .Where(e => e.Status == ScheduleStatus.Archived
                        && e.Recurrence == ScheduleRecurrence.None)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Start)
            .Take(MaxItems)
            .Select(e => new Response(
                e.Id,
                e.Title,
                e.Date,
                e.Start,
                e.End,
                e.Notes,
                e.CreatedAt,
                e.Recurrence,
                db.Set<ScheduleAttachment>()
                    .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready)))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<Response>>(items);
    }
}
