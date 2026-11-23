using System.Net.Http.Json;
using LifeScheduler.Api.Features.Completion;
using LifeScheduler.Api.Features.History;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Tests.Support;

namespace LifeScheduler.Tests.Features.History;

public sealed class HistoryEndpointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task History_ReturnsOnlyPastNonRecurringEntries_NewestFirst()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Вчера без времени", new DateOnly(2026, 11, 19));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Сегодня утром", new DateOnly(2026, 11, 20),
            new TimeOnly(10, 0), new TimeOnly(11, 0));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Сегодня вечером", new DateOnly(2026, 11, 20),
            new TimeOnly(13, 0), new TimeOnly(14, 0));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Сегодня без времени", new DateOnly(2026, 11, 20));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Завтра", new DateOnly(2026, 11, 21));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Зарядка", new DateOnly(2026, 11, 19),
            new TimeOnly(7, 0), new TimeOnly(7, 30), ScheduleRecurrence.Daily);

        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            "/api/schedules/history", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(history);
        Assert.Equal(
            ["Сегодня утром", "Вчера без времени"],
            history.Select(item => item.Title));
        Assert.All(history, item => Assert.Equal(ScheduleRecurrence.None, item.Recurrence));
    }

    [Fact]
    public async Task History_WhenEntryEndsExactlyNow_IsConsideredPast()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Закончилось ровно сейчас", new DateOnly(2026, 11, 20),
            new TimeOnly(11, 0), new TimeOnly(12, 0));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Ещё идёт", new DateOnly(2026, 11, 20),
            new TimeOnly(12, 0), new TimeOnly(13, 0));

        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            "/api/schedules/history", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(history);
        var item = Assert.Single(history);
        Assert.Equal("Закончилось ровно сейчас", item.Title);
    }

    [Fact]
    public async Task History_IncludesAttachmentCount()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var scheduleId = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Отчёт", new DateOnly(2026, 11, 19),
            new TimeOnly(9, 0), new TimeOnly(10, 0));

        await ScheduleTestHelpers.UploadAndConfirmAsync(
            client, scheduleId, "отчёт.pdf", "application/pdf", "pdf"u8.ToArray());

        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            "/api/schedules/history", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(history);
        var item = Assert.Single(history);
        Assert.Equal(1, item.AttachmentCount);
    }

    [Fact]
    public async Task History_FilterByQuery_MatchesTitleAndNotesIgnoringCase()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Сдать отчёт", new DateOnly(2026, 11, 19));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Позвонить клиенту", new DateOnly(2026, 11, 18),
            notes: "Обсудить ОТЧЁТ за квартал");
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Прогулка", new DateOnly(2026, 11, 18));

        var history = await GetHistoryAsync(client, "?q=отчёт");

        Assert.Equal(
            ["Сдать отчёт", "Позвонить клиенту"],
            history.Select(entry => entry.Title));
    }

    [Fact]
    public async Task History_FilterByDateRange_ReturnsOnlyEntriesInsideRange()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Вчера", new DateOnly(2026, 11, 19));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Позавчера", new DateOnly(2026, 11, 18));

        var history = await GetHistoryAsync(client, "?from=2026-11-19&to=2026-11-19");

        var item = Assert.Single(history);
        Assert.Equal("Вчера", item.Title);
    }

    [Fact]
    public async Task History_FilterByKindAndResult_ReturnsMatchingEntries()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Встреча", new DateOnly(2026, 11, 19));
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Пропущенная задача", new DateOnly(2026, 11, 19),
            kind: ScheduleKind.Task);

        var completedId = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Выполненная задача", new DateOnly(2026, 11, 18),
            kind: ScheduleKind.Task);
        var completeResponse = await client.PostAsJsonAsync(
            $"/api/schedules/{completedId}/completion",
            new CompleteScheduleRequest(new DateOnly(2026, 11, 18)));
        Assert.True(completeResponse.IsSuccessStatusCode);

        var missed = await GetHistoryAsync(client, "?kind=task&result=missed");
        var missedItem = Assert.Single(missed);
        Assert.Equal("Пропущенная задача", missedItem.Title);

        var completed = await GetHistoryAsync(client, "?result=completed");
        var completedItem = Assert.Single(completed);
        Assert.Equal("Выполненная задача", completedItem.Title);
    }

    [Fact]
    public async Task History_FilterByAttachments_ReturnsOnlyEntriesWithFiles()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var withFileId = await ScheduleTestHelpers.CreateEntryAsync(
            client, "С файлом", new DateOnly(2026, 11, 19));
        await ScheduleTestHelpers.UploadAndConfirmAsync(
            client, withFileId, "отчёт.pdf", "application/pdf", "pdf"u8.ToArray());

        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Без файла", new DateOnly(2026, 11, 19));

        var history = await GetHistoryAsync(client, "?hasAttachments=true");

        var item = Assert.Single(history);
        Assert.Equal("С файлом", item.Title);
    }

    private static async Task<List<ListHistory.Response>> GetHistoryAsync(
        HttpClient client,
        string query = "")
    {
        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            $"/api/schedules/history{query}", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(history);
        return history;
    }
}
