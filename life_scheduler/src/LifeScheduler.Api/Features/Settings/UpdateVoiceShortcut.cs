using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Settings;

public sealed record UpdateVoiceShortcutRequest(string Combo, ShortcutMode Mode, int HoldDelayMs);

public static class UpdateVoiceShortcut
{
    public static async Task<Results<Ok<VoiceShortcutSetting>, ValidationProblem>> Handle(
        UpdateVoiceShortcutRequest request,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var errors = VoiceShortcutRules.Validate(request.Combo, request.Mode, request.HoldDelayMs);

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        ShortcutCombo.TryNormalize(request.Combo, out var combo, out _);
        var setting = new VoiceShortcutSetting(combo, request.Mode, request.HoldDelayMs);
        var value = setting.ToStorage();

        var stored = await db.Settings
            .FirstOrDefaultAsync(existing => existing.Key == VoiceShortcutSetting.Key, ct);

        if (stored is null)
        {
            db.Settings.Add(new AppSetting
            {
                Key = VoiceShortcutSetting.Key,
                Value = value,
                UpdatedAt = time.GetUtcNow(),
            });
        }
        else
        {
            stored.Value = value;
            stored.UpdatedAt = time.GetUtcNow();
        }

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(setting);
    }
}
