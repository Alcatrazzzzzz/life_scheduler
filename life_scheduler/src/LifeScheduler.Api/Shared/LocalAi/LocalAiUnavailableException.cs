namespace LifeScheduler.Api.Shared.LocalAi;

public sealed class LocalAiUnavailableException : Exception
{
    public LocalAiUnavailableException()
        : base("Локальный AI отключён (LocalAi:Enabled=false).")
    {
    }
}
