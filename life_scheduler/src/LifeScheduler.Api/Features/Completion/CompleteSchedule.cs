using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Completion;

public sealed record CompleteScheduleRequest(DateOnly? Date);

public static class CompleteSchedule
{
    public static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        Guid id,
        CompleteScheduleRequest request,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var entry = await db.Schedules.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entry is null)
            return TypedResults.NotFound();

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var errors = CompletionRules.Validate(entry, request.Date, today, out var date);

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        if (entry.Kind == ScheduleKind.Task)
        {
            entry.CompletedAt ??= time.GetUtcNow();
            entry.Status = ScheduleStatus.Archived;
            entry.ArchivedAt ??= time.GetUtcNow();
        }
        else
        {
            var alreadyMarked = await db.OccurrenceCompletions
                .AnyAsync(c => c.ScheduleEntryId == id && c.Date == date, ct);

            if (!alreadyMarked)
            {
                db.OccurrenceCompletions.Add(new ScheduleOccurrenceCompletion
                {
                    Id = Guid.NewGuid(),
                    ScheduleEntryId = id,
                    Date = date,
                    CompletedAt = time.GetUtcNow(),
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
