namespace LifeScheduler.Api.Shared.LocalAi;

public static class LocalAiServiceCollectionExtensions
{
    public static IServiceCollection AddLocalAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LocalAiOptions>(
            configuration.GetSection(LocalAiOptions.SectionName));

        var enabled = configuration
            .GetValue<bool?>($"{LocalAiOptions.SectionName}:Enabled") ?? true;

        if (!enabled)
        {
            services.AddSingleton<IWhisperTranscriber, UnavailableWhisperTranscriber>();
            services.AddSingleton<ILlmFunctionCaller, UnavailableLlmFunctionCaller>();
            return services;
        }

        services.AddSingleton<IWhisperTranscriber, WhisperTranscriber>();
        services.AddSingleton<ILlmFunctionCaller, OnnxLlmFunctionCaller>();
        return services;
    }
}

internal sealed class UnavailableWhisperTranscriber : IWhisperTranscriber
{
    public Task<string> TranscribeAsync(
        Stream audio,
        string? contentType,
        string? fileName,
        CancellationToken ct) =>
        throw new LocalAiUnavailableException();
}

internal sealed class UnavailableLlmFunctionCaller : ILlmFunctionCaller
{
    public Task<LlmToolCall> CallAsync(
        string userText,
        string toolsSchema,
        CancellationToken ct) =>
        throw new LocalAiUnavailableException();
}
