namespace LifeScheduler.Api.Shared.LocalAi;

public sealed record LlmToolCall(string Name, string ArgumentsJson, string RawOutput);
