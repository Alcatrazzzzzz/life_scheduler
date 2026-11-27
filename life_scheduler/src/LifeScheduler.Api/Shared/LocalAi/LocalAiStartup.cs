using System.Diagnostics;
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

        var watch = Stopwatch.StartNew();
        services.GetRequiredService<IWhisperTranscriber>();
        logger.LogInformation(
            "voice.metrics stage=startup model=whisper load_ms={LoadMs}", watch.ElapsedMilliseconds);

        watch.Restart();
        services.GetRequiredService<ILlmFunctionCaller>();
        logger.LogInformation(
            "voice.metrics stage=startup model=onnx load_ms={LoadMs}", watch.ElapsedMilliseconds);

        logger.LogInformation("Локальные AI-модели загружены.");
    }
}
