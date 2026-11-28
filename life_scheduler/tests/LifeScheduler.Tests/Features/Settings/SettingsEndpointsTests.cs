using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LifeScheduler.Api.Features.Settings;
using LifeScheduler.Tests.Support;
using Microsoft.AspNetCore.Mvc;

namespace LifeScheduler.Tests.Features.Settings;

public sealed class SettingsEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task GetVoiceShortcut_WhenNothingStored_ReturnsDefaults()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/settings/voice-shortcut");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var setting = await response.Content.ReadFromJsonAsync<VoiceShortcutSetting>(JsonOptions);
        Assert.NotNull(setting);
        Assert.Equal(VoiceShortcutSetting.Default.Combo, setting.Combo);
        Assert.Equal(VoiceShortcutSetting.Default.Mode, setting.Mode);
        Assert.Equal(VoiceShortcutSetting.Default.HoldDelayMs, setting.HoldDelayMs);
    }

    [Fact]
    public async Task UpdateVoiceShortcut_WithValidData_NormalizesAndPersists()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new UpdateVoiceShortcutRequest("shift+ctrl+space", ShortcutMode.Hold, 400);

        var putResponse = await client.PutAsJsonAsync("/api/settings/voice-shortcut", request, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var saved = await putResponse.Content.ReadFromJsonAsync<VoiceShortcutSetting>(JsonOptions);
        Assert.NotNull(saved);
        Assert.Equal("Ctrl+Shift+Space", saved.Combo);
        Assert.Equal(ShortcutMode.Hold, saved.Mode);
        Assert.Equal(400, saved.HoldDelayMs);

        var getResponse = await client.GetAsync("/api/settings/voice-shortcut");
        var stored = await getResponse.Content.ReadFromJsonAsync<VoiceShortcutSetting>(JsonOptions);
        Assert.NotNull(stored);
        Assert.Equal(saved, stored);
    }

    [Fact]
    public async Task UpdateVoiceShortcut_Repeated_CreatesSingleRow()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await client.PutAsJsonAsync(
            "/api/settings/voice-shortcut",
            new UpdateVoiceShortcutRequest("Ctrl+K", ShortcutMode.Toggle, 350),
            JsonOptions);
        await client.PutAsJsonAsync(
            "/api/settings/voice-shortcut",
            new UpdateVoiceShortcutRequest("Ctrl+J", ShortcutMode.Toggle, 350),
            JsonOptions);

        var getResponse = await client.GetAsync("/api/settings/voice-shortcut");
        var stored = await getResponse.Content.ReadFromJsonAsync<VoiceShortcutSetting>(JsonOptions);

        Assert.NotNull(stored);
        Assert.Equal("Ctrl+J", stored.Combo);
    }

    [Fact]
    public async Task UpdateVoiceShortcut_WithoutModifier_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings/voice-shortcut",
            new UpdateVoiceShortcutRequest("Space", ShortcutMode.Toggle, 350),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Combo"));
    }

    [Fact]
    public async Task UpdateVoiceShortcut_WithDelayOutOfRange_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/settings/voice-shortcut",
            new UpdateVoiceShortcutRequest("Ctrl+Shift+Space", ShortcutMode.Hold, 5000),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("HoldDelayMs"));
    }
}
