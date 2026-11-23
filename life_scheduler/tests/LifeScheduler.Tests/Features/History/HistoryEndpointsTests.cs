using System.Net.Http.Json;
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
}
