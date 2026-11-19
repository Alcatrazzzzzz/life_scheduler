using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.History;

/// <summary>
/// Переводит прошедшие неповторяющиеся записи в историю. Вызывается фоновым сервисом
/// и вручную из тестов; также подхватывает строки, созданные до появления флага.
/// </summary>
public sealed class PastEntryArchiver(LifeSchedulerDbContext db, TimeProvider time)
{
    public async Task<int> ArchiveAsync(CancellationToken ct)
    {
        var localNow = time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(localNow);

        var candidates = await db.Schedules
            .Where(e => e.Status == ScheduleStatus.Active
                        && e.Recurrence == ScheduleRecurrence.None
                        && e.Date <= today)
            .ToListAsync(ct);

        var past = candidates
            .Where(e => PastEntryRules.IsPast(e.Date, e.Start, e.End, localNow))
            .ToList();

        if (past.Count == 0)
            return 0;

        var archivedAt = time.GetUtcNow();

        foreach (var entry in past)
        {
            entry.Status = ScheduleStatus.Archived;
            entry.ArchivedAt = archivedAt;
        }

        await db.SaveChangesAsync(ct);

        return past.Count;
    }
}
