namespace LifeScheduler.Api.Shared.LocalAi;

public sealed class LocalAiOptions
{
    public const string SectionName = "LocalAi";

    public bool Enabled { get; set; } = true;
    public string WhisperModelPath { get; set; } = "models/ggml-small-q8_0.bin";
    public string OnnxModelPath { get; set; } = "models/phi-4-mini-onnx-cpu-int4";
    public string WhisperLanguage { get; set; } = "ru";
    public int WhisperThreads { get; set; }
    public int OnnxMaxLength { get; set; } = 4096;
    public long MaxAudioBytes { get; set; } = 10 * 1024 * 1024;

    public static string ResolvePath(string contentRoot, string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(contentRoot, path);

    public static int ResolveThreads(int configured) =>
        configured > 0 ? configured : Environment.ProcessorCount;
}
