using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public sealed record UpdateScheduleRequest(
    string Title,
    DateOnly Date,
    TimeOnly? Start,
    TimeOnly? End,
    string? Notes,
    ScheduleRecurrence Recurrence = ScheduleRecurrence.None);

public static class UpdateSchedule
{
    public static async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        Guid id,
        UpdateScheduleRequest request,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title обязателен."];

        if ((request.Start is null) != (request.End is null))
            errors[nameof(request.End)] = ["Укажите и Start, и End — либо оставьте оба пустыми."];
        else if (request.Start is { } start && request.End is { } end && end <= start)
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
        entry.Recurrence = request.Recurrence;

        var isPast = entry.Recurrence == ScheduleRecurrence.None
                     && PastEntryRules.IsPast(
                         entry.Date, entry.Start, entry.End, time.GetLocalNow().DateTime);

        entry.Status = isPast ? ScheduleStatus.Archived : ScheduleStatus.Active;
        entry.ArchivedAt = isPast ? entry.ArchivedAt ?? time.GetUtcNow() : null;

        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
