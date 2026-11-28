namespace LifeScheduler.Api.Features.Settings;

public sealed class AppSetting
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
