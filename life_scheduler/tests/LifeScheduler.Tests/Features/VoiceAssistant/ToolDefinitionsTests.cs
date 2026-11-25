using System.Text.Json;
using LifeScheduler.Api.Features.VoiceAssistant;

namespace LifeScheduler.Tests.Features.VoiceAssistant;

public sealed class ToolDefinitionsTests
{
    [Fact]
    public void ToolsJson_ContainsEveryToolAndUnsupportedFallback()
    {
        using var document = JsonDocument.Parse(ToolDefinitions.ToolsJson);

        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);

        var names = document.RootElement
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();

        foreach (var name in ToolDefinitions.Names)
        {
            Assert.Contains(name, names);
        }

        Assert.Contains(ToolDefinitions.Unsupported, names);
    }
}
