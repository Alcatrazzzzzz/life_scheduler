using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Attachments;

public sealed record AddAttachmentRequest(string FileName, string ContentType, long SizeBytes);

public static class AddAttachment
{
    public sealed record UploadResponse(
        string Url,
        string Method,
        Dictionary<string, string> Headers);

    public sealed record Response(
        Guid Id,
        string FileName,
        string ContentType,
        long SizeBytes,
        DateTimeOffset ExpiresAt,
        UploadResponse Upload);

    public static async Task<Results<Created<Response>, ValidationProblem, NotFound>> Handle(
        Guid id,
        AddAttachmentRequest request,
        LifeSchedulerDbContext db,
        IFileStorage storage,
        TimeProvider time,
        ILogger<ScheduleEntry> logger,
        CancellationToken ct)
    {
        var scheduleExists = await db.Schedules.AnyAsync(e => e.Id == id, ct);
        if (!scheduleExists)
            return TypedResults.NotFound();

        var fileName = Path.GetFileName(request.FileName?.Trim() ?? string.Empty);
        var contentType = request.ContentType?.Trim() ?? string.Empty;

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(fileName))
            errors[nameof(request.FileName)] = ["Укажите имя файла."];
        else if (fileName.Length > 255)
            errors[nameof(request.FileName)] = ["Имя файла длиннее 255 символов."];

        if (string.IsNullOrWhiteSpace(contentType))
            errors[nameof(request.ContentType)] = ["Не указан MIME-тип файла."];
        else if (contentType.Length > 127)
            errors[nameof(request.ContentType)] = ["MIME-тип длиннее 127 символов."];

        if (request.SizeBytes <= 0)
            errors[nameof(request.SizeBytes)] = ["Размер файла должен быть больше нуля."];
        else if (request.SizeBytes > AttachmentLimits.MaxFileBytes)
            errors[nameof(request.SizeBytes)] = ["Файл больше 25 МБ."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        await RemoveStalePendingAsync(db, storage, time, logger, ct);

        var attachments = db.Set<ScheduleAttachment>().Where(a => a.ScheduleEntryId == id);

        var count = await attachments.CountAsync(ct);
        if (count >= AttachmentLimits.MaxFilesPerSchedule)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.FileName)] = ["К записи можно приложить не больше 10 файлов."],
            });
        }

        var totalBytes = await attachments.SumAsync(a => a.SizeBytes, ct);
        if (totalBytes + request.SizeBytes > AttachmentLimits.MaxTotalBytesPerSchedule)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.SizeBytes)] = ["Суммарный размер файлов записи превысит 100 МБ."],
            });
        }

        var attachmentId = Guid.NewGuid();
        var attachment = new ScheduleAttachment
        {
            Id = attachmentId,
            ScheduleEntryId = id,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = request.SizeBytes,
            StorageKey = StorageKeys.ForAttachment(id, attachmentId),
            Status = AttachmentStatus.Pending,
            CreatedAt = time.GetUtcNow(),
        };

        db.Add(attachment);
        await db.SaveChangesAsync(ct);

        var ticket = await storage.CreateUploadTicketAsync(
            attachment.Id, attachment.StorageKey, attachment.ContentType, ct);

        var response = new Response(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            ticket.ExpiresAt,
            new UploadResponse(ticket.Url, ticket.Method, new Dictionary<string, string>(ticket.Headers)));

        return TypedResults.Created($"/api/schedules/{id}", response);
    }

    /// <summary>Брошенные загрузки: строки Pending старше часа с их объектами.</summary>
    private static async Task RemoveStalePendingAsync(
        LifeSchedulerDbContext db,
        IFileStorage storage,
        TimeProvider time,
        ILogger<ScheduleEntry> logger,
        CancellationToken ct)
    {
        var cutoff = time.GetUtcNow().AddHours(-1);
        var pending = await db.Set<ScheduleAttachment>()
            .Where(a => a.Status == AttachmentStatus.Pending)
            .ToListAsync(ct);

        // SQLite не сравнивает DateTimeOffset в SQL — отбираем брошенные строки в памяти.
        var stale = pending.Where(a => a.CreatedAt < cutoff).ToList();

        if (stale.Count == 0)
            return;

        db.RemoveRange(stale);
        await db.SaveChangesAsync(ct);

        foreach (var attachment in stale)
        {
            try
            {
                await storage.DeleteAsync(attachment.StorageKey, ct);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Не удалось удалить брошенный объект {StorageKey}",
                    attachment.StorageKey);
            }
        }
    }
}
