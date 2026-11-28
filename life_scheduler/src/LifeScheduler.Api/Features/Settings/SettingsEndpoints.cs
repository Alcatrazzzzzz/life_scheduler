namespace LifeScheduler.Api.Features.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/voice-shortcut", GetVoiceShortcut.Handle);
        group.MapPut("/voice-shortcut", UpdateVoiceShortcut.Handle);

        return app;
    }
}
