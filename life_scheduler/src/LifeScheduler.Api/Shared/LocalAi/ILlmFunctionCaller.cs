namespace LifeScheduler.Api.Shared.LocalAi;

public interface ILlmFunctionCaller
{
    Task<LlmToolCall> CallAsync(string userText, string toolsSchema, CancellationToken ct);
}
