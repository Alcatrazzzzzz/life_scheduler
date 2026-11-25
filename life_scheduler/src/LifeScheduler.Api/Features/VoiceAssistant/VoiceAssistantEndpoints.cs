namespace LifeScheduler.Api.Features.VoiceAssistant;

public static class VoiceAssistantEndpoints
{
    public static IEndpointRouteBuilder MapVoiceAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assistant").WithTags("VoiceAssistant");
        ProcessCommand.Map(group);

        return app;
    }
}
