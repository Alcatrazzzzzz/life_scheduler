using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Completion;

public static class UncompleteSchedule
{
    public static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        Guid id,
        DateOnly? date,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var entry = await db.Schedules.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entry is null)
            return TypedResults.NotFound();

        var localNow = time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(localNow);
        var errors = CompletionRules.Validate(entry, date, today, out var day);

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        if (entry.Kind == ScheduleKind.Task)
        {
            entry.CompletedAt = null;

            // Задача возвращается в календарь, если её день ещё не закончился;
            // иначе снова становится пропущенной.
            var missed = ScheduleTypeRules.IsArchivable(entry.Kind, entry.Recurrence)
                         && ScheduleTypeRules.HasPassed(
                             entry.Kind, entry.Date, entry.Start, entry.End, localNow);

            entry.Status = missed ? ScheduleStatus.Archived : ScheduleStatus.Active;
            entry.ArchivedAt = missed ? entry.ArchivedAt ?? time.GetUtcNow() : null;
        }
        else
        {
            var completion = await db.OccurrenceCompletions
                .FirstOrDefaultAsync(c => c.ScheduleEntryId == id && c.Date == day, ct);

            if (completion is not null)
                db.OccurrenceCompletions.Remove(completion);
        }

        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
