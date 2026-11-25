namespace LifeScheduler.Api.Shared.LocalAi;

public sealed class AudioDecodeException : Exception
{
    public AudioDecodeException(string message)
        : base(message)
    {
    }

    public AudioDecodeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
