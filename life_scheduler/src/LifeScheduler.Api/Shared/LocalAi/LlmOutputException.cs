namespace LifeScheduler.Api.Shared.LocalAi;

public sealed class LlmOutputException : Exception
{
    public LlmOutputException(string rawOutput)
        : base("LLM вернула ответ вне ожидаемой схемы.")
    {
        RawOutput = rawOutput;
    }

    public string RawOutput { get; }
}
