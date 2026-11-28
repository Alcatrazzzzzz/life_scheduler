namespace LifeScheduler.Api.Features.Settings;

public static class VoiceShortcutRules
{
    public static Dictionary<string, string[]> Validate(string? combo, ShortcutMode mode, int holdDelayMs)
    {
        var errors = new Dictionary<string, string[]>();

        if (!ShortcutCombo.TryNormalize(combo, out _, out var comboError))
            errors["Combo"] = [comboError!];

        if (!Enum.IsDefined(mode))
            errors["Mode"] = ["Неизвестный режим срабатывания."];

        if (holdDelayMs is < VoiceShortcutSetting.MinHoldDelayMs or > VoiceShortcutSetting.MaxHoldDelayMs)
        {
            errors["HoldDelayMs"] =
            [
                $"Задержка должна быть от {VoiceShortcutSetting.MinHoldDelayMs} " +
                $"до {VoiceShortcutSetting.MaxHoldDelayMs} мс.",
            ];
        }

        return errors;
    }
}
