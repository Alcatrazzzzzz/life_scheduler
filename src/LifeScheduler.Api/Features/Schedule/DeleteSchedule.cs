using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public static class DeleteSchedule
{
    public static async Task<Results<NoContent, NotFound>> Handle(
        Guid id,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var entry = await db.Schedules.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entry is null)
            return TypedResults.NotFound();

        db.Schedules.Remove(entry);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
