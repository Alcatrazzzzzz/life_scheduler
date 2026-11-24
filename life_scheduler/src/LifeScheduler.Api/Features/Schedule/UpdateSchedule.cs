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
    ScheduleRecurrence Recurrence = ScheduleRecurrence.None,
    ScheduleKind Kind = ScheduleKind.Event,
    string? Reward = null,
    Guid? StackAfterId = null);

public static class UpdateSchedule
{
    public static async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        Guid id,
        UpdateScheduleRequest request,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var errors = ScheduleTypeRules.Validate(request.Kind, request.Recurrence);

        foreach (var (field, messages) in HabitRules.ValidateReward(request.Kind, request.Reward))
            errors[field] = messages;

        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title обязателен."];

        if ((request.Start is null) != (request.End is null))
            errors[nameof(request.End)] = ["Укажите и Start, и End — либо оставьте оба пустыми."];
        else if (request.Start is { } start && request.End is { } end && end <= start)
            errors[nameof(request.End)] = ["End должен быть позже Start."];

        var entry = await db.Schedules.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entry is null)
            return TypedResults.NotFound();

        if (request.StackAfterId is { } stackId)
        {
            var target = stackId == id
                ? entry
                : await db.Schedules.AsNoTracking().FirstOrDefaultAsync(e => e.Id == stackId, ct);

            HabitRules.ValidateStackTarget(request.Kind, stackId, target, errors);

            if (request.Kind == ScheduleKind.Habit && target is not null)
            {
                var links = await db.Schedules
                    .AsNoTracking()
                    .Where(e => e.Kind == ScheduleKind.Habit)
                    .Select(e => new { e.Id, e.StackAfterId })
                    .ToDictionaryAsync(e => e.Id, e => e.StackAfterId, ct);

                if (HabitRules.WouldCreateCycle(id, stackId, links))
                    errors[nameof(request.StackAfterId)] =
                        ["Цепочка привычек не может образовывать цикл."];
            }
        }

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var oldDate = entry.Date;
        var oldRecurrence = entry.Recurrence;

        entry.Title = request.Title.Trim();
        entry.Kind = request.Kind;
        entry.Date = request.Date;
        entry.Start = request.Start;
        entry.End = request.End;
        entry.Notes = request.Notes;
        entry.Reward = HabitRules.NormalizeReward(request.Reward);
        entry.StackAfterId = HabitRules.NormalizeStackAfterId(request.Kind, request.StackAfterId);
        entry.Recurrence = request.Recurrence;

        if (entry.Kind != ScheduleKind.Task)
            entry.CompletedAt = null;

        if (entry.Kind != ScheduleKind.Habit)
        {
            var dependents = await db.Schedules
                .Where(e => e.StackAfterId == id)
                .ToListAsync(ct);

            foreach (var dependent in dependents)
                dependent.StackAfterId = null;
        }

        await RemoveInvalidCompletionsAsync(db, entry, oldDate, oldRecurrence, ct);

        var localNow = time.GetLocalNow().DateTime;
        var isPast = ScheduleTypeRules.IsArchivable(entry.Kind, entry.Recurrence)
                     && (entry.CompletedAt is not null
                         || ScheduleTypeRules.HasPassed(
                             entry.Kind, entry.Date, entry.Start, entry.End, localNow));

        entry.Status = isPast ? ScheduleStatus.Archived : ScheduleStatus.Active;
        entry.ArchivedAt = isPast ? entry.ArchivedAt ?? time.GetUtcNow() : null;

        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task RemoveInvalidCompletionsAsync(
        LifeSchedulerDbContext db,
        ScheduleEntry entry,
        DateOnly oldDate,
        ScheduleRecurrence oldRecurrence,
        CancellationToken ct)
    {
        var marks = await db.OccurrenceCompletions
            .Where(c => c.ScheduleEntryId == entry.Id)
            .ToListAsync(ct);

        if (marks.Count == 0)
            return;

        if (entry.Kind != ScheduleKind.Habit)
        {
            db.OccurrenceCompletions.RemoveRange(marks);
            return;
        }

        if (entry.Date == oldDate && entry.Recurrence == oldRecurrence)
            return;

        var invalid = marks
            .Where(m => !OccurrenceRules.IsOccurrence(entry.Date, entry.Recurrence, m.Date))
            .ToList();

        db.OccurrenceCompletions.RemoveRange(invalid);
    }
}
