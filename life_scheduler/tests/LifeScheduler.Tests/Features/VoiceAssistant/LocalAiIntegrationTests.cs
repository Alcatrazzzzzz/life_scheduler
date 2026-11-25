using LifeScheduler.Api.Features.VoiceAssistant;
using LifeScheduler.Api.Shared.LocalAi;
using LifeScheduler.Tests.Support;

namespace LifeScheduler.Tests.Features.VoiceAssistant;

public sealed class LocalAiIntegrationTests
{
    [LocalAiFact]
    [Trait("Category", "LocalAi")]
    public async Task Llm_TextCommand_ReturnsLogHabitToolCall()
    {
        var apiRoot = FindApiRoot();
        var options = new LocalAiOptions();
        var customModelPath = Environment.GetEnvironmentVariable("LOCALAI_ONNX_PATH");

        if (!string.IsNullOrWhiteSpace(customModelPath))
        {
            options.OnnxModelPath = customModelPath;
        }

        var modelPath = LocalAiOptions.ResolvePath(apiRoot, options.OnnxModelPath);

        Assert.True(
            Directory.Exists(modelPath),
            $"Нет ONNX-модели: {modelPath}. Скачайте её по инструкции из README.md.");

        using var caller = new OnnxLlmFunctionCaller(options, apiRoot, TimeProvider.System);

        var call = await caller.CallAsync(
            "Отметь планёрку",
            ToolDefinitions.ToolsJson,
            CancellationToken.None);

        Assert.Equal(ToolDefinitions.LogHabit, call.Name);
        Assert.Contains("план", call.ArgumentsJson, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindApiRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "LifeScheduler.Api");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не найден каталог src/LifeScheduler.Api.");
    }
}
