using Microsoft.Extensions.Options;

namespace LifeScheduler.Api.Shared.LocalAi;

public static class LocalAiStartup
{
    public static void Initialize(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<LocalAiOptions>>().Value;

        if (!options.Enabled)
        {
            return;
        }

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("LocalAi");

        logger.LogInformation("Загрузка локальных AI-моделей…");
        services.GetRequiredService<IWhisperTranscriber>();
        services.GetRequiredService<ILlmFunctionCaller>();
        logger.LogInformation("Локальные AI-модели загружены.");
    }
}
