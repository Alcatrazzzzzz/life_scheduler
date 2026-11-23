using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Attachments;

public static class DeleteAttachment
{
    public static async Task<Results<NoContent, NotFound>> Handle(
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

        // Сначала убираем метаданные: осиротевший объект восстановим, потерянную строку — нет.
        db.Remove(attachment);
        await db.SaveChangesAsync(ct);

        try
        {
            await storage.DeleteAsync(attachment.StorageKey, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Не удалось удалить объект {StorageKey}", attachment.StorageKey);
        }

        return TypedResults.NoContent();
    }
}
