using LifeScheduler.Api.Shared.LocalAi;

namespace LifeScheduler.Tests.Support;

public sealed class FakeWhisperTranscriber(string transcript) : IWhisperTranscriber
{
    public int Calls { get; private set; }

    public Task<string> TranscribeAsync(
        Stream audio,
        string? contentType,
        string? fileName,
        CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(transcript);
    }
}

public sealed class FakeLlmFunctionCaller : ILlmFunctionCaller
{
    private readonly Queue<Func<LlmToolCall>> _responses = new();

    public int Calls { get; private set; }

    public static FakeLlmFunctionCaller Returning(LlmToolCall call) =>
        new FakeLlmFunctionCaller().Then(call);

    public FakeLlmFunctionCaller Then(LlmToolCall call)
    {
        _responses.Enqueue(() => call);
        return this;
    }

    public FakeLlmFunctionCaller ThenFailure(string rawOutput)
    {
        _responses.Enqueue(() => throw new LlmOutputException(rawOutput));
        return this;
    }

    public Task<LlmToolCall> CallAsync(
        string userText,
        string toolsSchema,
        CancellationToken ct)
    {
        Calls++;

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeLlmFunctionCaller: ответ не задан.");
        }

        return Task.FromResult(_responses.Dequeue()());
    }

    public static LlmToolCall ToolCall(string name, string argumentsJson) =>
        new(name, argumentsJson, $"{{\"name\":\"{name}\",\"arguments\":{argumentsJson}}}");
}
