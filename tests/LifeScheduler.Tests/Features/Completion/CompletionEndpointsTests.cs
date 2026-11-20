using System.Net;
using System.Net.Http.Json;
using LifeScheduler.Api.Features.Completion;
using LifeScheduler.Api.Features.History;
using LifeScheduler.Api.Features.Schedule;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Tests.Support;

namespace LifeScheduler.Tests.Features.Completion;

public sealed class CompletionEndpointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 20);

    [Fact]
    public async Task CompleteTask_HidesItFromCalendarAndAddsToHistory()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await Scheduler.CreateEntryAsync(
            client, "Сдать отчёт", Today, null, null, kind: ScheduleKind.Task);

        var response = await CompleteAsync(client, id, Today);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            $"/api/schedules?from={Today:yyyy-MM-dd}&to={Today:yyyy-MM-dd}",
            ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(list);
        Assert.DoesNotContain(list, item => item.Id == id);

        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            "/api/schedules/history", ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(history);
        var item = Assert.Single(history);
        Assert.Equal(id, item.Id);
        Assert.Equal(ScheduleKind.Task, item.Kind);
        Assert.Equal(HistoryResult.Completed, item.Result);
    }

    [Fact]
    public async Task CompleteTask_ForFutureDate_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await Scheduler.CreateEntryAsync(
            client, "Завтрашняя задача", Today.AddDays(1), null, null,
            kind: ScheduleKind.Task);

        var response = await CompleteAsync(client, id, null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteEvent_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await Scheduler.CreateEntryAsync(client, "Встреча", Today);

        var response = await CompleteAsync(client, id, Today);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteHabit_MarksOnlyThatDay()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await Scheduler.CreateEntryAsync(
            client, "Зарядка", new DateOnly(2026, 11, 18), null, null,
            ScheduleRecurrence.Daily, ScheduleKind.Habit);

        var response = await CompleteAsync(client, id, new DateOnly(2026, 11, 19));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            "/api/schedules?from=2026-11-18&to=2026-11-20",
            ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(list);
        Assert.Equal(
            [new DateOnly(2026, 11, 18), new DateOnly(2026, 11, 20)],
            list.Select(item => item.Date));

        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            "/api/schedules/history", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(history);
        var mark = Assert.Single(history);
        Assert.Equal(id, mark.Id);
        Assert.Equal(new DateOnly(2026, 11, 19), mark.Date);
        Assert.Equal(ScheduleKind.Habit, mark.Kind);
        Assert.Equal(HistoryResult.Completed, mark.Result);
    }

    [Fact]
    public async Task CompleteHabit_OnNonOccurrenceDay_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        // 2026-11-18 — среда, значит четверг 19 ноября не входит в серию.
        var id = await Scheduler.CreateEntryAsync(
            client, "Планёрка", new DateOnly(2026, 11, 18), null, null,
            ScheduleRecurrence.Weekly, ScheduleKind.Habit);

        var response = await CompleteAsync(client, id, new DateOnly(2026, 11, 19));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UncompleteTask_ReturnsItToCalendar()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await Scheduler.CreateEntryAsync(
            client, "Сдать отчёт", Today, null, null, kind: ScheduleKind.Task);

        await CompleteAsync(client, id, Today);

        var response = await client.DeleteAsync(
            $"/api/schedules/{id}/completion?date={Today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            $"/api/schedules?from={Today:yyyy-MM-dd}&to={Today:yyyy-MM-dd}",
            ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(list);
        Assert.Contains(list, item => item.Id == id);
    }

    [Fact]
    public async Task UncompleteHabit_RemovesMarkAndRestoresOccurrence()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await Scheduler.CreateEntryAsync(
            client, "Зарядка", new DateOnly(2026, 11, 18), null, null,
            ScheduleRecurrence.Daily, ScheduleKind.Habit);
        var day = new DateOnly(2026, 11, 19);

        await CompleteAsync(client, id, day);

        var response = await client.DeleteAsync(
            $"/api/schedules/{id}/completion?date={day:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            $"/api/schedules?from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}",
            ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(list);
        Assert.Contains(list, item => item.Id == id && item.Date == day);

        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            "/api/schedules/history", ScheduleTestHelpers.JsonOptions);
        Assert.NotNull(history);
        Assert.Empty(history);
    }

    private static Task<HttpResponseMessage> CompleteAsync(
        HttpClient client,
        Guid id,
        DateOnly? date) =>
        client.PostAsJsonAsync(
            $"/api/schedules/{id}/completion",
            new CompleteScheduleRequest(date));

    private static class Scheduler
    {
        public static Task<Guid> CreateEntryAsync(
            HttpClient client,
            string title,
            DateOnly date,
            TimeOnly? start = null,
            TimeOnly? end = null,
            ScheduleRecurrence recurrence = ScheduleRecurrence.None,
            ScheduleKind kind = ScheduleKind.Event) =>
            ScheduleTestHelpers.CreateEntryAsync(
                client, title, date, start, end, recurrence, kind);
    }
}
