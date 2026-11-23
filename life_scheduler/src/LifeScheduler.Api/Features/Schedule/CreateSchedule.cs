using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LifeScheduler.Api.Features.Schedule;

public sealed record CreateScheduleRequest(
    string Title,
    DateOnly Date,
    TimeOnly? Start,
    TimeOnly? End,
    string? Notes,
    ScheduleRecurrence Recurrence = ScheduleRecurrence.None,
    ScheduleKind Kind = ScheduleKind.Event);

public static class CreateSchedule
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

    public static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        CreateScheduleRequest request,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var errors = ScheduleTypeRules.Validate(request.Kind, request.Recurrence);

        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title обязателен."];

        if ((request.Start is null) != (request.End is null))
            errors[nameof(request.End)] = ["Укажите и Start, и End — либо оставьте оба пустыми."];
        else if (request.Start is { } start && request.End is { } end && end <= start)
            errors[nameof(request.End)] = ["End должен быть позже Start."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var localNow = time.GetLocalNow().DateTime;
        var isPast = ScheduleTypeRules.IsArchivable(request.Kind, request.Recurrence)
                     && ScheduleTypeRules.HasPassed(
                         request.Kind, request.Date, request.Start, request.End, localNow);

        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Kind = request.Kind,
            Date = request.Date,
            Start = request.Start,
            End = request.End,
            Notes = request.Notes,
            Recurrence = request.Recurrence,
            Status = isPast ? ScheduleStatus.Archived : ScheduleStatus.Active,
            ArchivedAt = isPast ? time.GetUtcNow() : null,
            CreatedAt = time.GetUtcNow(),
        };

        db.Schedules.Add(entry);
        await db.SaveChangesAsync(ct);

        var response = new Response(
            entry.Id,
            entry.Title,
            entry.Date,
            entry.Start,
            entry.End,
            entry.Notes,
            entry.CreatedAt,
            entry.Recurrence,
            entry.Kind,
            AttachmentCount: 0);

        return TypedResults.Created($"/api/schedules/{entry.Id}", response);
    }
}
