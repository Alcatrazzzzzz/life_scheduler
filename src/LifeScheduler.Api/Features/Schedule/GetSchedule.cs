using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public static class GetSchedule
{
    public sealed record AttachmentResponse(
        Guid Id,
        string FileName,
        string ContentType,
        long SizeBytes,
        DateTimeOffset CreatedAt);

    public sealed record Response(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeOnly? Start,
        TimeOnly? End,
        string? Notes,
        DateTimeOffset CreatedAt,
        ScheduleRecurrence Recurrence,
        IReadOnlyList<AttachmentResponse> Attachments);

    public static async Task<Results<Ok<Response>, NotFound>> Handle(
        Guid id,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var entry = await db.Schedules
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Date,
                e.Start,
                e.End,
                e.Notes,
                e.CreatedAt,
                e.Recurrence,
            })
            .FirstOrDefaultAsync(ct);

        if (entry is null)
            return TypedResults.NotFound();

        var attachments = await db.Set<ScheduleAttachment>()
            .AsNoTracking()
            .Where(a => a.ScheduleEntryId == id && a.Status == AttachmentStatus.Ready)
            .ToListAsync(ct);

        // Сортировка по DateTimeOffset в SQLite не транслируется — упорядочиваем в памяти.
        var attachmentResponses = attachments
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AttachmentResponse(
                a.Id, a.FileName, a.ContentType, a.SizeBytes, a.CreatedAt))
            .ToList();

        var response = new Response(
            entry.Id,
            entry.Title,
            entry.Date,
            entry.Start,
            entry.End,
            entry.Notes,
            entry.CreatedAt,
            entry.Recurrence,
            attachmentResponses);

        return TypedResults.Ok(response);
    }
}
