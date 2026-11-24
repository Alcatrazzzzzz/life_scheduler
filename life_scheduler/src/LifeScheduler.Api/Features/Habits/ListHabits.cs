using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Habits;

public static class ListHabits
{
    private const int DefaultWindowDays = 91;
    private const int MinWindowDays = 7;
    private const int MaxWindowDays = 365;

    public sealed record DayResponse(DateOnly Date, bool Scheduled, bool Completed);

    public sealed record HabitResponse(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeOnly? Start,
        TimeOnly? End,
        string? Notes,
        ScheduleRecurrence Recurrence,
        string? Reward,
        Guid? StackAfterId,
        string? StackAfterTitle,
        int CurrentStreak,
        int LongestStreak,
        bool MissedTwice,
        bool LastOccurrenceMissed,
        DateOnly? LastMissedDate,
        DateOnly? NextOccurrenceDate,
        bool TodayScheduled,
        bool TodayDone,
        double? CompletionRate,
        IReadOnlyList<DayResponse> Days);

    public sealed record Response(DateOnly Today, IReadOnlyList<HabitResponse> Habits);

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", Handle)
            .WithName("ListHabits")
            .Produces<Response>()
            .ProducesValidationProblem();

    public static async Task<Results<Ok<Response>, ValidationProblem>> Handle(
        int? days,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var window = days ?? DefaultWindowDays;
        if (window is < MinWindowDays or > MaxWindowDays)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(days)] = [$"Окно должно быть от {MinWindowDays} до {MaxWindowDays} дней."],
            });
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var from = today.AddDays(-(window - 1));
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var to = weekStart.AddDays(6);

        var habits = await db.Schedules
            .AsNoTracking()
            .Where(h => h.Kind == ScheduleKind.Habit && h.Status == ScheduleStatus.Active)
            .Select(h => new
            {
                h.Id,
                h.Title,
                h.Date,
                h.Start,
                h.End,
                h.Notes,
                h.Recurrence,
                h.Reward,
                h.StackAfterId,
            })
            .ToListAsync(ct);

        if (habits.Count == 0)
            return TypedResults.Ok(new Response(today, []));

        var habitIds = habits.Select(h => h.Id).ToList();

        var completions = await db.OccurrenceCompletions
            .AsNoTracking()
            .Where(c => habitIds.Contains(c.ScheduleEntryId))
            .Select(c => new { c.ScheduleEntryId, c.Date })
            .ToListAsync(ct);

        var completedByHabit = completions
            .GroupBy(c => c.ScheduleEntryId)
            .ToDictionary(group => group.Key, group => group.Select(c => c.Date).ToHashSet());

        var titles = habits.ToDictionary(h => h.Id, h => h.Title);
        var result = new List<HabitResponse>(habits.Count);

        foreach (var habit in habits)
        {
            var completed = completedByHabit.TryGetValue(habit.Id, out var dates)
                ? dates
                : [];
            var streaks = HabitStatistics.Streaks(habit.Date, habit.Recurrence, completed, today);
            var misses = HabitStatistics.FindMisses(habit.Date, habit.Recurrence, completed, today);

            var stackAfterTitle = habit.StackAfterId is { } stackId
                                  && titles.TryGetValue(stackId, out var title)
                ? title
                : null;

            result.Add(new HabitResponse(
                habit.Id,
                habit.Title,
                habit.Date,
                habit.Start,
                habit.End,
                habit.Notes,
                habit.Recurrence,
                habit.Reward,
                habit.StackAfterId,
                stackAfterTitle,
                streaks.Current,
                streaks.Longest,
                misses.MissedTwice,
                misses.LastOccurrenceMissed,
                misses.LastMissedDate,
                HabitStatistics.NextOccurrence(habit.Date, habit.Recurrence, today),
                OccurrenceRules.IsOccurrence(habit.Date, habit.Recurrence, today),
                completed.Contains(today),
                HabitStatistics.CompletionRate(
                    habit.Date, habit.Recurrence, completed, from, to, today),
                HabitStatistics.Window(habit.Date, habit.Recurrence, completed, from, to)
                    .Select(day => new DayResponse(day.Date, day.Scheduled, day.Completed))
                    .ToList()));
        }

        return TypedResults.Ok(new Response(
            today,
            result.OrderBy(h => h.Title, StringComparer.Ordinal).ToList()));
    }
}
