using System.Net;
using System.Net.Http.Json;
using LifeScheduler.Api.Features.Schedule;
using Microsoft.AspNetCore.Mvc;

namespace LifeScheduler.Tests.Features.Schedule;

public sealed class ScheduleEndpointsTests
{
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
        var created = await response.Content.ReadFromJsonAsync<CreateSchedule.Response>();
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
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
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
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("End"));
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
        var entry = await response.Content.ReadFromJsonAsync<GetSchedule.Response>();
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

        var updated = await client.GetFromJsonAsync<GetSchedule.Response>($"/api/schedules/{id}");
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
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
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

    private static async Task<Guid> CreateEntryAsync(HttpClient client)
    {
        var request = new CreateScheduleRequest(
            "Утренняя пробежка",
            new DateOnly(2026, 11, 20),
            new TimeOnly(9, 0),
            new TimeOnly(10, 30),
            "5 км");

        var response = await client.PostAsJsonAsync("/api/schedules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateSchedule.Response>();
        Assert.NotNull(created);
        return created.Id;
    }
}
