using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;

namespace LifeScheduler.Tests.Shared;

public sealed class HabitStatisticsTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);

    [Fact]
    public void Streaks_Daily_CountsThroughUnfinishedToday()
    {
        var completed = Set(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 11),
            new DateOnly(2026, 11, 12));

        var streaks = HabitStatistics.Streaks(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, completed, new DateOnly(2026, 11, 13));

        Assert.Equal(3, streaks.Current);
        Assert.Equal(3, streaks.Longest);
    }

    [Fact]
    public void Streaks_Daily_BreaksOnMissedDay()
    {
        var completed = Set(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 11),
            new DateOnly(2026, 11, 13));

        var streaks = HabitStatistics.Streaks(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, completed, new DateOnly(2026, 11, 14));

        Assert.Equal(1, streaks.Current);
        Assert.Equal(2, streaks.Longest);
    }

    [Fact]
    public void Streaks_Weekly_CountsWeeklyOccurrences()
    {
        var completed = Set(Monday, Monday.AddDays(7), Monday.AddDays(14));

        var streaks = HabitStatistics.Streaks(
            Monday, ScheduleRecurrence.Weekly, completed, new DateOnly(2026, 11, 20));

        Assert.Equal(3, streaks.Current);
        Assert.Equal(3, streaks.Longest);
    }

    [Fact]
    public void Streaks_Weekly_BreaksWhenWeekIsMissed()
    {
        var completed = Set(Monday, Monday.AddDays(14));

        var streaks = HabitStatistics.Streaks(
            Monday, ScheduleRecurrence.Weekly, completed, new DateOnly(2026, 11, 20));

        Assert.Equal(1, streaks.Current);
        Assert.Equal(1, streaks.Longest);
    }

    [Fact]
    public void Streaks_IgnoresDaysBeforeSeed()
    {
        var completed = Set(Monday.AddDays(-1), Monday);

        var streaks = HabitStatistics.Streaks(
            Monday, ScheduleRecurrence.Daily, completed, Monday);

        Assert.Equal(1, streaks.Current);
        Assert.Equal(1, streaks.Longest);
    }

    [Fact]
    public void FindMisses_DetectsTwoMissedOccurrences()
    {
        var misses = HabitStatistics.FindMisses(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, Set(), new DateOnly(2026, 11, 14));

        Assert.True(misses.LastOccurrenceMissed);
        Assert.True(misses.MissedTwice);
        Assert.Equal(new DateOnly(2026, 11, 13), misses.LastMissedDate);
    }

    [Fact]
    public void FindMisses_LastOccurrenceCompleted_IsClean()
    {
        var completed = Set(new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 13));

        var misses = HabitStatistics.FindMisses(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, completed, new DateOnly(2026, 11, 14));

        Assert.False(misses.LastOccurrenceMissed);
        Assert.False(misses.MissedTwice);
        Assert.Null(misses.LastMissedDate);
    }

    [Fact]
    public void FindMisses_OneMissedAfterCompletedNeighbour_IsNotTwice()
    {
        var completed = Set(new DateOnly(2026, 11, 12));

        var misses = HabitStatistics.FindMisses(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, completed, new DateOnly(2026, 11, 14));

        Assert.True(misses.LastOccurrenceMissed);
        Assert.False(misses.MissedTwice);
        Assert.Equal(new DateOnly(2026, 11, 13), misses.LastMissedDate);
    }

    [Fact]
    public void FindMisses_NoPastOccurrences_IsClean()
    {
        var misses = HabitStatistics.FindMisses(
            new DateOnly(2026, 11, 14), ScheduleRecurrence.Daily, Set(), new DateOnly(2026, 11, 14));

        Assert.False(misses.LastOccurrenceMissed);
        Assert.False(misses.MissedTwice);
        Assert.Null(misses.LastMissedDate);
    }

    [Fact]
    public void Window_MarksScheduledAndCompletedDays()
    {
        var completed = Set(new DateOnly(2026, 11, 11));

        var days = HabitStatistics.Window(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, completed,
            new DateOnly(2026, 11, 8), new DateOnly(2026, 11, 12));

        Assert.Equal(5, days.Count);
        Assert.False(days[0].Scheduled);
        Assert.False(days[1].Scheduled);
        Assert.True(days[2].Scheduled);
        Assert.True(days[3].Completed);
        Assert.True(days[4].Scheduled);
        Assert.False(days[4].Completed);
    }

    [Fact]
    public void CompletionRate_UsesOnlyPastOccurrences()
    {
        var completed = Set(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 12));

        var rate = HabitStatistics.CompletionRate(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, completed,
            new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 13));

        Assert.Equal(2d / 3d, rate);
    }

    [Fact]
    public void CompletionRate_NoDueDays_ReturnsNull()
    {
        var rate = HabitStatistics.CompletionRate(
            new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, Set(),
            new DateOnly(2026, 11, 13), new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 13));

        Assert.Null(rate);
    }

    [Fact]
    public void NextOccurrence_Daily_ReturnsTodayOrSeed()
    {
        Assert.Equal(
            new DateOnly(2026, 11, 12),
            HabitStatistics.NextOccurrence(
                new DateOnly(2026, 11, 10), ScheduleRecurrence.Daily, new DateOnly(2026, 11, 12)));
        Assert.Equal(
            new DateOnly(2026, 11, 15),
            HabitStatistics.NextOccurrence(
                new DateOnly(2026, 11, 15), ScheduleRecurrence.Daily, new DateOnly(2026, 11, 12)));
    }

    [Fact]
    public void NextOccurrence_Weekly_AlignsToSeedWeekday()
    {
        var next = HabitStatistics.NextOccurrence(
            Monday, ScheduleRecurrence.Weekly, new DateOnly(2026, 11, 3));

        Assert.Equal(Monday.AddDays(7), next);
    }

    private static HashSet<DateOnly> Set(params DateOnly[] dates) => [.. dates];
}
