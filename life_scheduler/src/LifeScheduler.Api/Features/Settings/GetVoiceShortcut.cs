using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Settings;

public static class GetVoiceShortcut
{
    public static async Task<Ok<VoiceShortcutSetting>> Handle(
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var stored = await db.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(setting => setting.Key == VoiceShortcutSetting.Key, ct);

        var result = stored is null
            ? VoiceShortcutSetting.Default
            : VoiceShortcutSetting.FromStorage(stored.Value);

        return TypedResults.Ok(result);
    }
}
