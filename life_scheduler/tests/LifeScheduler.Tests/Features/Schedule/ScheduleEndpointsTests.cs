using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LifeScheduler.Api.Features.Schedule;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Tests.Support;
using Microsoft.AspNetCore.Mvc;

namespace LifeScheduler.Tests.Features.Schedule;

public sealed class ScheduleEndpointsTests
{
    // Совпадает с настройками API: enum'ы в JSON — строки в camelCase.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    [Fact]
    public async Task CreateSchedule_WithValidData_ReturnsCreated()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Утренняя пробежка",
            new DateOnly(2026, 11, 20),
            new TimeOnly(9, 0),
            new TimeOnly(10, 30),
            "5 км");

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateSchedule.Response>(JsonOptions);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Утренняя пробежка", created.Title);
        Assert.Equal(new DateOnly(2026, 11, 20), created.Date);
        Assert.Equal(new TimeOnly(9, 0), created.Start);
        Assert.Equal(new TimeOnly(10, 30), created.End);
        Assert.Equal("5 км", created.Notes);
    }

    [Fact]
    public async Task CreateSchedule_WithoutTitle_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "",
            new DateOnly(2026, 11, 20),
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            null);

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Title"));
    }

    [Fact]
    public async Task CreateSchedule_WhenEndBeforeStart_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Встреча",
            new DateOnly(2026, 11, 20),
            new TimeOnly(10, 0),
            new TimeOnly(9, 0),
            null);

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("End"));
    }

    [Fact]
    public async Task CreateSchedule_WithoutTime_ReturnsCreated()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Разобрать почту",
            new DateOnly(2026, 11, 20),
            null,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateSchedule.Response>(JsonOptions);
        Assert.NotNull(created);
        Assert.Null(created.Start);
        Assert.Null(created.End);
        Assert.Equal(new DateOnly(2026, 11, 20), created.Date);
    }

    [Fact]
    public async Task CreateSchedule_WhenOnlyStartProvided_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Встреча",
            new DateOnly(2026, 11, 20),
            new TimeOnly(10, 0),
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("End"));
    }

    [Fact]
    public async Task ListSchedule_ShowsEntriesWithoutTimeBeforeTimedEntries()
    {
        // Полдень фиксирован, иначе запись на 18:00 успевает уйти в историю и тест зависит от часов прогона.
        using var factory = new ScheduleApiFactory(
            new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var date = new DateOnly(2026, 11, 20);
        await CreateEntryAsync(client, date, "Вечерняя прогулка", new TimeOnly(18, 0), new TimeOnly(19, 0));
        await CreateEntryAsync(client, date, "Разобрать почту", null, null);

        var response = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            $"/api/schedules?from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(2, response.Count);
        Assert.Equal("Разобрать почту", response[0].Title);
        Assert.Null(response[0].Start);
        Assert.Equal("Вечерняя прогулка", response[1].Title);
    }

    [Fact]
    public async Task UpdateSchedule_CanRemoveTime()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var id = await CreateEntryAsync(client);

        var request = new UpdateScheduleRequest(
            "Утренняя пробежка",
            new DateOnly(2026, 11, 20),
            null,
            null,
            null);

        var response = await client.PutAsJsonAsync($"/api/schedules/{id}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await client.GetFromJsonAsync<GetSchedule.Response>($"/api/schedules/{id}", JsonOptions);
        Assert.NotNull(updated);
        Assert.Null(updated.Start);
        Assert.Null(updated.End);
    }

    [Fact]
    public async Task GetSchedule_WhenExists_ReturnsOk()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var id = await CreateEntryAsync(client);

        var response = await client.GetAsync($"/api/schedules/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = await response.Content.ReadFromJsonAsync<GetSchedule.Response>(JsonOptions);
        Assert.NotNull(entry);
        Assert.Equal(id, entry.Id);
        Assert.Equal("Утренняя пробежка", entry.Title);
    }

    [Fact]
    public async Task GetSchedule_WhenMissing_ReturnsNotFound()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/schedules/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSchedule_WhenExists_ReturnsNoContent()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var id = await CreateEntryAsync(client);

        var request = new UpdateScheduleRequest(
            "Вечерняя прогулка",
            new DateOnly(2026, 11, 21),
            new TimeOnly(18, 0),
            new TimeOnly(19, 0),
            "Парк");

        var response = await client.PutAsJsonAsync($"/api/schedules/{id}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await client.GetFromJsonAsync<GetSchedule.Response>($"/api/schedules/{id}", JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Вечерняя прогулка", updated.Title);
        Assert.Equal(new DateOnly(2026, 11, 21), updated.Date);
        Assert.Equal(new TimeOnly(18, 0), updated.Start);
        Assert.Equal(new TimeOnly(19, 0), updated.End);
        Assert.Equal("Парк", updated.Notes);
    }

    [Fact]
    public async Task UpdateSchedule_WhenMissing_ReturnsNotFound()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new UpdateScheduleRequest(
            "Вечерняя прогулка",
            new DateOnly(2026, 11, 21),
            new TimeOnly(18, 0),
            new TimeOnly(19, 0),
            null);

        var response = await client.PutAsJsonAsync($"/api/schedules/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSchedule_WhenEndBeforeStart_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var id = await CreateEntryAsync(client);

        var request = new UpdateScheduleRequest(
            "Встреча",
            new DateOnly(2026, 11, 20),
            new TimeOnly(10, 0),
            new TimeOnly(9, 0),
            null);

        var response = await client.PutAsJsonAsync($"/api/schedules/{id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("End"));
    }

    [Fact]
    public async Task DeleteSchedule_WhenExists_ReturnsNoContent()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();
        var id = await CreateEntryAsync(client);

        var response = await client.DeleteAsync($"/api/schedules/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var afterDelete = await client.GetAsync($"/api/schedules/{id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteSchedule_WhenMissing_ReturnsNotFound()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/schedules/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListSchedule_WhenDailyRecurrence_ReturnsOccurrenceForEachDayInRange()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Зарядка",
            new DateOnly(2026, 11, 10),
            new TimeOnly(7, 0),
            new TimeOnly(7, 30),
            null,
            ScheduleRecurrence.Daily);

        var createResponse = await client.PostAsJsonAsync("/api/schedules", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSchedule.Response>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(ScheduleRecurrence.Daily, created.Recurrence);

        var response = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            "/api/schedules?from=2026-11-20&to=2026-11-22", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(3, response.Count);
        Assert.Equal(
            [
                new DateOnly(2026, 11, 20),
                new DateOnly(2026, 11, 21),
                new DateOnly(2026, 11, 22),
            ],
            response.Select(item => item.Date));
        Assert.All(response, item => Assert.Equal(created.Id, item.Id));
        Assert.All(response, item => Assert.Equal(ScheduleRecurrence.Daily, item.Recurrence));
    }

    [Fact]
    public async Task ListSchedule_WhenWeeklyRecurrence_ReturnsOnlySeedWeekday()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Планёрка",
            new DateOnly(2026, 11, 20),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            null,
            ScheduleRecurrence.Weekly);

        var createResponse = await client.PostAsJsonAsync("/api/schedules", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // 2026-11-20 — пятница, значит в диапазоне ожидаются 20 ноября, 27 ноября и 4 декабря.
        var response = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            "/api/schedules?from=2026-11-01&to=2026-12-05", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(
            [
                new DateOnly(2026, 11, 20),
                new DateOnly(2026, 11, 27),
                new DateOnly(2026, 12, 4),
            ],
            response.Select(item => item.Date));
    }

    [Fact]
    public async Task ListSchedule_WhenWeeklyRecurrenceStartsBeforeRange_AlignsToSeedWeekday()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Планёрка",
            new DateOnly(2026, 11, 18),
            null,
            null,
            null,
            ScheduleRecurrence.Weekly);

        var createResponse = await client.PostAsJsonAsync("/api/schedules", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // 2026-11-18 — среда: в неделе 23–29 ноября ожидается только 25 ноября.
        var response = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            "/api/schedules?from=2026-11-23&to=2026-11-29", JsonOptions);

        Assert.NotNull(response);
        var item = Assert.Single(response);
        Assert.Equal(new DateOnly(2026, 11, 25), item.Date);
    }

    [Fact]
    public async Task DeleteSchedule_WhenRecurring_RemovesWholeSeries()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Зарядка",
            new DateOnly(2026, 11, 20),
            null,
            null,
            null,
            ScheduleRecurrence.Daily);

        var createResponse = await client.PostAsJsonAsync("/api/schedules", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSchedule.Response>(JsonOptions);
        Assert.NotNull(created);

        var deleteResponse = await client.DeleteAsync($"/api/schedules/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var response = await client.GetFromJsonAsync<List<ListSchedule.Response>>(
            "/api/schedules?from=2026-11-20&to=2026-11-25", JsonOptions);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task CreateSchedule_WithoutRecurrence_ReturnsNone()
    {
        using var factory = new ScheduleApiFactory();
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var request = new CreateScheduleRequest(
            "Разобрать почту",
            new DateOnly(2026, 11, 20),
            null,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateSchedule.Response>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(ScheduleRecurrence.None, created.Recurrence);
    }

    private static async Task<Guid> CreateEntryAsync(HttpClient client)
    {
        return await CreateEntryAsync(
            client,
            new DateOnly(2026, 11, 20),
            "Утренняя пробежка",
            new TimeOnly(9, 0),
            new TimeOnly(10, 30));
    }

    private static async Task<Guid> CreateEntryAsync(
        HttpClient client,
        DateOnly date,
        string title,
        TimeOnly? start,
        TimeOnly? end)
    {
        var request = new CreateScheduleRequest(title, date, start, end, "5 км");

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateSchedule.Response>(JsonOptions);
        Assert.NotNull(created);
        return created.Id;
    }
}
