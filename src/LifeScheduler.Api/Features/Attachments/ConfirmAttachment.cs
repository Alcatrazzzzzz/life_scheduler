using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Attachments;

public static class ConfirmAttachment
{
    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Handle(
        Guid id,
        Guid attachmentId,
        LifeSchedulerDbContext db,
        IFileStorage storage,
        ILogger<ScheduleEntry> logger,
        CancellationToken ct)
    {
        var attachment = await db.Set<ScheduleAttachment>()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ScheduleEntryId == id, ct);

        if (attachment is null)
            return TypedResults.NotFound();

        if (attachment.Status == AttachmentStatus.Ready)
            return TypedResults.NoContent();

        var stored = await storage.StatAsync(attachment.StorageKey, ct);

        if (stored is null)
        {
            // Объекта нет — строка Pending бесполезна, повторная загрузка начнётся с нового тикета.
            db.Remove(attachment);
            await db.SaveChangesAsync(ct);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Файл не загружен",
                detail: "Объект не найден в хранилище. Начните загрузку заново.");
        }

        var mismatch = await ValidateAsync(db, attachment, stored, ct);

        if (mismatch is not null)
        {
            await TryDeleteObjectAsync(storage, attachment.StorageKey, logger, ct);
            db.Remove(attachment);
            await db.SaveChangesAsync(ct);

            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Загрузка отклонена",
                detail: mismatch);
        }

        attachment.SizeBytes = stored.SizeBytes;
        attachment.Status = AttachmentStatus.Ready;
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task<string?> ValidateAsync(
        LifeSchedulerDbContext db,
        ScheduleAttachment attachment,
        StoredObject stored,
        CancellationToken ct)
    {
        if (stored.SizeBytes > AttachmentLimits.MaxFileBytes)
            return "Файл больше 25 МБ.";

        if (stored.SizeBytes != attachment.SizeBytes)
            return "Размер загруженного файла не совпал с заявленным.";

        if (stored.ContentType is not null &&
            !string.Equals(stored.ContentType, attachment.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return "MIME-тип загруженного файла не совпал с заявленным.";
        }

        var readyBytes = await db.Set<ScheduleAttachment>()
            .Where(a => a.ScheduleEntryId == attachment.ScheduleEntryId
                        && a.Id != attachment.Id
                        && a.Status == AttachmentStatus.Ready)
            .SumAsync(a => a.SizeBytes, ct);

        if (readyBytes + stored.SizeBytes > AttachmentLimits.MaxTotalBytesPerSchedule)
            return "Суммарный размер файлов записи превысит 100 МБ.";

        return null;
    }

    private static async Task TryDeleteObjectAsync(
        IFileStorage storage,
        string storageKey,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            await storage.DeleteAsync(storageKey, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Не удалось удалить отклонённый объект {StorageKey}", storageKey);
        }
    }
}
