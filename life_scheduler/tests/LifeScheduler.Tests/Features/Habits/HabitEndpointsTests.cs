using System.Net;
using System.Net.Http.Json;
using LifeScheduler.Api.Features.Completion;
using LifeScheduler.Api.Features.Habits;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Tests.Support;

namespace LifeScheduler.Tests.Features.Habits;

public sealed class HabitEndpointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 20);

    [Fact]
    public async Task ListHabits_ReturnsEnvelopeStatsAndWindow()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Зарядка", new DateOnly(2026, 11, 18),
            recurrence: ScheduleRecurrence.Daily, kind: ScheduleKind.Habit);

        foreach (var date in new[] { new DateOnly(2026, 11, 18), new DateOnly(2026, 11, 19) })
        {
            var complete = await client.PostAsJsonAsync(
                $"/api/schedules/{id}/completion", new CompleteScheduleRequest(date));
            Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);
        }

        var response = await client.GetFromJsonAsync<ListHabits.Response>(
            "/api/habits?days=7", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(Today, response.Today);
        var habit = Assert.Single(response.Habits);

        Assert.Equal(id, habit.Id);
        Assert.Equal(2, habit.CurrentStreak);
        Assert.Equal(2, habit.LongestStreak);
        Assert.True(habit.TodayScheduled);
        Assert.False(habit.TodayDone);
        Assert.False(habit.LastOccurrenceMissed);
        Assert.False(habit.MissedTwice);
        Assert.Equal(1d, habit.CompletionRate);
        Assert.Equal(new DateOnly(2026, 11, 14), habit.Days[0].Date);
        Assert.Equal(new DateOnly(2026, 11, 22), habit.Days[^1].Date);
    }

    [Fact]
    public async Task ListHabits_MarksTwoMissedOccurrences()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Зарядка", new DateOnly(2026, 11, 10),
            recurrence: ScheduleRecurrence.Daily, kind: ScheduleKind.Habit);

        var response = await client.GetFromJsonAsync<ListHabits.Response>(
            "/api/habits?days=7", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(response);
        var habit = Assert.Single(response.Habits);

        Assert.True(habit.LastOccurrenceMissed);
        Assert.True(habit.MissedTwice);
        Assert.Equal(new DateOnly(2026, 11, 19), habit.LastMissedDate);
        Assert.Equal(0, habit.CurrentStreak);
    }

    [Fact]
    public async Task ListHabits_ReturnsStackTriggerTitleAndSkipsNonHabits()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        await ScheduleTestHelpers.CreateEntryAsync(client, "Встреча", Today);
        await ScheduleTestHelpers.CreateEntryAsync(client, "Задача", Today, kind: ScheduleKind.Task);

        var first = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Зарядка", Today,
            recurrence: ScheduleRecurrence.Daily, kind: ScheduleKind.Habit);
        await ScheduleTestHelpers.CreateEntryAsync(
            client, "Душ", Today,
            recurrence: ScheduleRecurrence.Daily, kind: ScheduleKind.Habit,
            stackAfterId: first);

        var response = await client.GetFromJsonAsync<ListHabits.Response>(
            "/api/habits", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(2, response.Habits.Count);
        Assert.Equal("Зарядка", response.Habits.Single(h => h.Title == "Душ").StackAfterTitle);
    }

    [Fact]
    public async Task ListHabits_Empty_ReturnsEnvelope()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<ListHabits.Response>(
            "/api/habits", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(Today, response.Today);
        Assert.Empty(response.Habits);
    }

    [Fact]
    public async Task ListHabits_InvalidWindow_ReturnsValidationProblem()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/habits?days=3");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
