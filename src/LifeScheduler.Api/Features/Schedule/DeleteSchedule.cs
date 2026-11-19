using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public static class DeleteSchedule
{
    public static async Task<Results<NoContent, NotFound>> Handle(
        Guid id,
        LifeSchedulerDbContext db,
        IFileStorage storage,
        ILogger<ScheduleEntry> logger,
        CancellationToken ct)
    {
        var entry = await db.Schedules.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entry is null)
            return TypedResults.NotFound();

        // Каскад SQLite удалит только строки, объекты в хранилище убираем руками.
        var storageKeys = await db.Set<ScheduleAttachment>()
            .Where(a => a.ScheduleEntryId == id)
            .Select(a => a.StorageKey)
            .ToListAsync(ct);

        db.Schedules.Remove(entry);
        await db.SaveChangesAsync(ct);

        foreach (var storageKey in storageKeys)
        {
            try
            {
                await storage.DeleteAsync(storageKey, ct);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Не удалось удалить объект {StorageKey}", storageKey);
            }
        }

        return TypedResults.NoContent();
    }
}
