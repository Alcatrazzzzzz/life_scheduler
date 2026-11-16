using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public sealed record UpdateScheduleRequest(
    string Title,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End,
    string? Notes);

public static class UpdateSchedule
{
    public static async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        Guid id,
        UpdateScheduleRequest request,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title обязателен."];

        if (request.End <= request.Start)
            errors[nameof(request.End)] = ["End должен быть позже Start."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var entry = await db.Schedules.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entry is null)
            return TypedResults.NotFound();

        entry.Title = request.Title.Trim();
        entry.Date = request.Date;
        entry.Start = request.Start;
        entry.End = request.End;
        entry.Notes = request.Notes;

        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
