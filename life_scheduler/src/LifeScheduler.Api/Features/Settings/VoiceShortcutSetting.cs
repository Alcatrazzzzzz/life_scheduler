using System.Text.Json;
using System.Text.Json.Serialization;

namespace LifeScheduler.Api.Features.Settings;

public enum ShortcutMode
{
    Toggle,
    Hold,
}

public sealed record VoiceShortcutSetting(string Combo, ShortcutMode Mode, int HoldDelayMs)
{
    public const string Key = "voice.shortcut";
    public const int MinHoldDelayMs = 0;
    public const int MaxHoldDelayMs = 2000;

    public static VoiceShortcutSetting Default { get; } =
        new("Ctrl+Shift+Space", ShortcutMode.Toggle, 350);

    public static readonly JsonSerializerOptions StorageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static VoiceShortcutSetting FromStorage(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<VoiceShortcutSetting>(value, StorageJsonOptions) ?? Default;
        }
        catch (JsonException)
        {
            return Default;
        }
    }

    public string ToStorage() => JsonSerializer.Serialize(this, StorageJsonOptions);
}
