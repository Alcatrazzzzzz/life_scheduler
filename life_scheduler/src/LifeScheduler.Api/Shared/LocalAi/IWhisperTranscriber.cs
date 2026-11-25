namespace LifeScheduler.Api.Shared.LocalAi;

public interface IWhisperTranscriber
{
    Task<string> TranscribeAsync(
        Stream audio,
        string? contentType,
        string? fileName,
        CancellationToken ct);
}
