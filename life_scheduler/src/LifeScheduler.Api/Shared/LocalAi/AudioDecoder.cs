using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LifeScheduler.Api.Shared.LocalAi;

internal static class AudioDecoder
{
    private const int TargetSampleRate = 16_000;

    public static float[] ToPcm16kMono(
        Stream audio,
        string? contentType,
        string? fileName,
        TimeSpan maxDuration)
    {
        using var buffer = new MemoryStream();
        audio.CopyTo(buffer);
        buffer.Position = 0;

        try
        {
            using var reader = OpenReader(buffer, contentType, fileName);
            ISampleProvider samples = reader.ToSampleProvider();

            if (samples.WaveFormat.Channels == 2)
            {
                samples = new StereoToMonoSampleProvider(samples)
                {
                    LeftVolume = 0.5f,
                    RightVolume = 0.5f,
                };
            }
            else if (samples.WaveFormat.Channels != 1)
            {
                throw new AudioDecodeException("Поддерживается только моно или стерео аудио.");
            }

            if (samples.WaveFormat.SampleRate != TargetSampleRate)
            {
                samples = new WdlResamplingSampleProvider(samples, TargetSampleRate);
            }

            var maxSamples = (int)(maxDuration.TotalSeconds * TargetSampleRate);
            var result = new List<float>(Math.Min(maxSamples, TargetSampleRate * 10));
            var block = new float[TargetSampleRate];

            int read;
            while ((read = samples.Read(block)) > 0)
            {
                result.AddRange(block.AsSpan(0, read).ToArray());

                if (result.Count > maxSamples)
                {
                    throw new AudioDecodeException(
                        $"Запись длиннее {maxDuration.TotalSeconds:0} секунд.");
                }
            }

            if (result.Count == 0)
            {
                throw new AudioDecodeException("В аудиофайле не найдено звука.");
            }

            return result.ToArray();
        }
        catch (AudioDecodeException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new AudioDecodeException(
                "Не удалось декодировать аудио. Для webm/ogg нужны кодеки Windows Media Foundation " +
                "(Web Media Extensions); WAV работает всегда.",
                ex);
        }
    }

    private static WaveStream OpenReader(
        MemoryStream stream,
        string? contentType,
        string? fileName)
    {
        var mediaType = NormalizeMediaType(contentType);

        if (HasRiffHeader(stream) || IsWaveMediaType(mediaType))
        {
            return new WaveFileReader(stream);
        }

        return new StreamMediaFoundationReader(
            stream,
            new MediaFoundationReader.MediaFoundationReaderSettings { RequestFloatOutput = true },
            mediaType ?? "audio/webm",
            string.IsNullOrWhiteSpace(fileName) ? "recording.webm" : fileName);
    }

    private static bool HasRiffHeader(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        stream.Position = 0;

        return read == header.Length
               && header[..4].SequenceEqual("RIFF"u8)
               && header[8..12].SequenceEqual("WAVE"u8);
    }

    private static bool IsWaveMediaType(string? mediaType) =>
        mediaType is "audio/wav" or "audio/x-wav" or "audio/wave";

    internal static string? NormalizeMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        var separator = contentType.IndexOf(';');
        var mediaType = (separator >= 0 ? contentType[..separator] : contentType).Trim();

        return mediaType.Length == 0 ? null : mediaType.ToLowerInvariant();
    }
}
