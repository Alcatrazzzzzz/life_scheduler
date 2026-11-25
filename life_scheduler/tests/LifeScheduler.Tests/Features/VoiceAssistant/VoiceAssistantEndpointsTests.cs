using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.LocalAi;
using LifeScheduler.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LifeScheduler.Tests.Features.VoiceAssistant;

public sealed class VoiceAssistantEndpointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 20);

    [Fact]
    public async Task Command_LogHabit_RoutesToCompletion()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();

        var llm = FakeLlmFunctionCaller.Returning(
            FakeLlmFunctionCaller.ToolCall(
                "log_habit",
                """{"habit_name":"планёрку","completed_at":null}"""));

        using var host = CreateAssistantHost(factory, "отметь планёрку", llm);
        using var client = host.CreateClient();

        var habitId = await ScheduleTestHelpers.CreateEntryAsync(
            client,
            "Планёрка",
            Today.AddDays(-3),
            recurrence: ScheduleRecurrence.Daily,
            kind: ScheduleKind.Habit);

        var response = await client.PostAsJsonAsync(
            "/api/assistant/command",
            new { text = "отметь планёрку" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("log_habit", payload.GetProperty("action").GetProperty("name").GetString());
        Assert.Equal("habit_logged", payload.GetProperty("result").GetProperty("kind").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LifeSchedulerDbContext>();
        Assert.True(await db.OccurrenceCompletions.AnyAsync(
            c => c.ScheduleEntryId == habitId && c.Date == Today));
    }

    [Fact]
    public async Task Command_MultipartAudio_UsesTranscriber()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();

        var whisper = new FakeWhisperTranscriber("какие у меня привычки");
        var llm = FakeLlmFunctionCaller.Returning(
            FakeLlmFunctionCaller.ToolCall("list_habits", """{"filter":"all"}"""));

        using var host = CreateAssistantHost(factory, whisper, llm);
        using var client = host.CreateClient();

        using var content = new MultipartFormDataContent();
        using var audio = new ByteArrayContent([0x1A, 0x45, 0xDF, 0xA3]);
        audio.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
        content.Add(audio, "audio", "voice.webm");

        var response = await client.PostAsync("/api/assistant/command", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, whisper.Calls);
    }

    [Fact]
    public async Task Command_InvalidLlmOutput_RetriesOnce()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();

        var llm = new FakeLlmFunctionCaller()
            .ThenFailure("это не JSON")
            .Then(FakeLlmFunctionCaller.ToolCall(
                "log_habit",
                """{"habit_name":"планёрку","completed_at":null}"""));

        using var host = CreateAssistantHost(factory, "отметь планёрку", llm);
        using var client = host.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client,
            "Планёрка",
            Today.AddDays(-3),
            recurrence: ScheduleRecurrence.Daily,
            kind: ScheduleKind.Habit);

        var response = await client.PostAsJsonAsync(
            "/api/assistant/command",
            new { text = "отметь планёрку" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, llm.Calls);
    }

    [Fact]
    public async Task Command_HabitNotFound_Returns404WithSuggestion()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();

        var llm = FakeLlmFunctionCaller.Returning(
            FakeLlmFunctionCaller.ToolCall(
                "log_habit",
                """{"habit_name":"несуществующая","completed_at":null}"""));

        using var host = CreateAssistantHost(factory, "отметь несуществующую", llm);
        using var client = host.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client,
            "Планёрка",
            Today.AddDays(-3),
            recurrence: ScheduleRecurrence.Daily,
            kind: ScheduleKind.Habit);

        var response = await client.PostAsJsonAsync(
            "/api/assistant/command",
            new { text = "отметь несуществующую" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Не нашёл", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Command_UnsupportedIntent_Returns422()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();

        var llm = FakeLlmFunctionCaller.Returning(
            FakeLlmFunctionCaller.ToolCall(
                "unsupported",
                """{"reason":"удаление не поддерживается"}"""));

        using var host = CreateAssistantHost(factory, "удали привычку", llm);
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/assistant/command",
            new { text = "удали привычку" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Command_EmptyText_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();

        using var host = CreateAssistantHost(
            factory,
            "не используется",
            FakeLlmFunctionCaller.Returning(
                FakeLlmFunctionCaller.ToolCall("list_habits", """{"filter":"all"}""")));
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/assistant/command",
            new { text = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateAssistantHost(
        ScheduleApiFactory factory,
        string transcript,
        FakeLlmFunctionCaller llm) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IWhisperTranscriber>();
                services.RemoveAll<ILlmFunctionCaller>();
                services.AddSingleton<IWhisperTranscriber>(new FakeWhisperTranscriber(transcript));
                services.AddSingleton<ILlmFunctionCaller>(llm);
            }));

    private static WebApplicationFactory<Program> CreateAssistantHost(
        ScheduleApiFactory factory,
        FakeWhisperTranscriber whisper,
        FakeLlmFunctionCaller llm) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IWhisperTranscriber>();
                services.RemoveAll<ILlmFunctionCaller>();
                services.AddSingleton<IWhisperTranscriber>(whisper);
                services.AddSingleton<ILlmFunctionCaller>(llm);
            }));
}
