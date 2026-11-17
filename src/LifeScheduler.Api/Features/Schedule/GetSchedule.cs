using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public static class GetSchedule
{
    public sealed record Response(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeOnly? Start,
        TimeOnly? End,
        string? Notes,
        DateTimeOffset CreatedAt,
        ScheduleRecurrence Recurrence);

    public static async Task<Results<Ok<Response>, NotFound>> Handle(
        Guid id,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var response = await db.Schedules
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new Response(
                e.Id, e.Title, e.Date, e.Start, e.End, e.Notes, e.CreatedAt, e.Recurrence))
            .FirstOrDefaultAsync(ct);

        if (response is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(response);
    }
}
