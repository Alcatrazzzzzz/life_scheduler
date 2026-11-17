using LifeScheduler.Api.Shared.Data;
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
        DateTimeOffset CreatedAt);

    public static async Task<Ok<IReadOnlyList<Response>>> Handle(
        DateOnly? from,
        DateOnly? to,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var query = db.Schedules.AsNoTracking();

        if (from is not null)
            query = query.Where(e => e.Date >= from.Value);

        if (to is not null)
            query = query.Where(e => e.Date <= to.Value);

        var items = await query
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Start.HasValue ? 1 : 0)
            .ThenBy(e => e.Start)
            .Select(e => new Response(
                e.Id, e.Title, e.Date, e.Start, e.End, e.Notes, e.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<Response>>(items);
    }
}
