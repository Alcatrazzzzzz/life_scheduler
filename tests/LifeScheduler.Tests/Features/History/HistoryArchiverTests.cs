using System.Net;
using System.Net.Http.Json;
using LifeScheduler.Api.Features.History;
using LifeScheduler.Api.Features.Schedule;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LifeScheduler.Tests.Features.History;

public sealed class HistoryArchiverTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Archiver_MarksLegacyPastEntryAsArchived()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Старая запись", new DateOnly(2026, 11, 19));

        // Имитируем строку, созданную до появления фонового архиватора.
        await SetStatusAsync(factory, id, ScheduleStatus.Active, archivedAt: null);

        await factory.ArchivePastEntriesAsync();

        var entry = await LoadAsync(factory, id);
        Assert.Equal(ScheduleStatus.Archived, entry.Status);
        Assert.NotNull(entry.ArchivedAt);

        var history = await client.GetFromJsonAsync<List<ListHistory.Response>>(
            "/api/schedules/history", ScheduleTestHelpers.JsonOptions);

        Assert.NotNull(history);
        Assert.Contains(history, item => item.Id == id);
    }

    [Fact]
    public async Task Archiver_DoesNotTouchFutureOrRecurringEntries()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var futureId = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Будущая", new DateOnly(2026, 11, 21));
        var recurringId = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Зарядка", new DateOnly(2026, 11, 19),
            new TimeOnly(7, 0), new TimeOnly(8, 0), ScheduleRecurrence.Daily);

        await factory.ArchivePastEntriesAsync();

        var future = await LoadAsync(factory, futureId);
        var recurring = await LoadAsync(factory, recurringId);

        Assert.Equal(ScheduleStatus.Active, future.Status);
        Assert.Equal(ScheduleStatus.Active, recurring.Status);
        Assert.Null(future.ArchivedAt);
        Assert.Null(recurring.ArchivedAt);
    }

    [Fact]
    public async Task CreateSchedule_WhenEntryIsPast_ArchivesImmediately()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Вчера", new DateOnly(2026, 11, 19));

        var entry = await LoadAsync(factory, id);

        Assert.Equal(ScheduleStatus.Archived, entry.Status);
        Assert.NotNull(entry.ArchivedAt);
    }

    [Fact]
    public async Task UpdateSchedule_WhenArchivedEntryMovesToFuture_ReturnsToActive()
    {
        using var factory = new ScheduleApiFactory(Now);
        factory.InitializeDatabase();
        var client = factory.CreateClient();

        var id = await ScheduleTestHelpers.CreateEntryAsync(
            client, "Вчера", new DateOnly(2026, 11, 19));

        var request = new UpdateScheduleRequest(
            "Вчера",
            new DateOnly(2026, 11, 25),
            null,
            null,
            null);

        var response = await client.PutAsJsonAsync($"/api/schedules/{id}", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var entry = await LoadAsync(factory, id);

        Assert.Equal(ScheduleStatus.Active, entry.Status);
        Assert.Null(entry.ArchivedAt);
    }

    private static async Task SetStatusAsync(
        ScheduleApiFactory factory,
        Guid id,
        ScheduleStatus status,
        DateTimeOffset? archivedAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LifeSchedulerDbContext>();
        var entry = await db.Schedules.SingleAsync(e => e.Id == id);

        entry.Status = status;
        entry.ArchivedAt = archivedAt;
        await db.SaveChangesAsync();
    }

    private static async Task<ScheduleEntry> LoadAsync(ScheduleApiFactory factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LifeSchedulerDbContext>();
        return await db.Schedules.AsNoTracking().SingleAsync(e => e.Id == id);
    }
}
