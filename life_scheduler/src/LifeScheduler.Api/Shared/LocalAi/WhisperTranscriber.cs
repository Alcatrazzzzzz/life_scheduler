using System.Text;
using Microsoft.Extensions.Options;
using Whisper.net;

namespace LifeScheduler.Api.Shared.LocalAi;

public sealed class WhisperTranscriber : IWhisperTranscriber, IDisposable
{
    private static readonly TimeSpan MaxAudioDuration = TimeSpan.FromSeconds(60);

    private readonly WhisperFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _language;
    private readonly int _threads;

    public WhisperTranscriber(
        IOptions<LocalAiOptions> options,
        IHostEnvironment environment,
        ILogger<WhisperTranscriber>? logger = null)
        : this(options.Value, environment.ContentRootPath, logger)
    {
    }

    public WhisperTranscriber(
        LocalAiOptions options,
        string contentRoot,
        ILogger<WhisperTranscriber>? logger = null)
    {
        var path = LocalAiOptions.ResolvePath(contentRoot, options.WhisperModelPath);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Модель Whisper не найдена: {path}. Положите ggml-medium.bin в " +
                "src/LifeScheduler.Api/models/ и см. README.md.");
        }

        var factoryOptions = WhisperFactoryOptions.Default;
        factoryOptions.UseGpu = false;

        try
        {
            _factory = WhisperFactory.FromPath(path, factoryOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Не удалось загрузить модель Whisper из {path}. См. README.md.", ex);
        }

        _language = options.WhisperLanguage;
        _threads = LocalAiOptions.ResolveThreads(options.WhisperThreads);

        logger?.LogInformation("Модель Whisper загружена: {Path}", path);
    }

    public async Task<string> TranscribeAsync(
        Stream audio,
        string? contentType,
        string? fileName,
        CancellationToken ct)
    {
        var samples = AudioDecoder.ToPcm16kMono(audio, contentType, fileName, MaxAudioDuration);

        await _gate.WaitAsync(ct);
        try
        {
            var builder = _factory.CreateBuilder()
                .WithThreads(_threads)
                .WithNoContext();

            if (!string.IsNullOrWhiteSpace(_language))
            {
                builder = builder.WithLanguage(_language);
            }

            using var processor = builder.Build();
            var text = new StringBuilder();

            await foreach (var segment in processor.ProcessAsync(samples, ct))
            {
                text.Append(segment.Text);
            }

            return text.ToString().Trim();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
        _gate.Dispose();
    }
}
