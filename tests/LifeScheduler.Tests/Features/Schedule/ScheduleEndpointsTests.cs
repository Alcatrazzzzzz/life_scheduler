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
}
