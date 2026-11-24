using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public static class GetSchedule
{
    public sealed record AttachmentResponse(
        Guid Id,
        string FileName,
        string ContentType,
        long SizeBytes,
        DateTimeOffset CreatedAt);

    public sealed record HabitDayResponse(DateOnly Date, bool Scheduled, bool Completed);

    public sealed record Response(
        Guid Id,
        string Title,
        DateOnly Date,
        DateOnly Today,
        TimeOnly? Start,
        TimeOnly? End,
        string? Notes,
        DateTimeOffset CreatedAt,
        ScheduleRecurrence Recurrence,
        ScheduleKind Kind,
        DateTimeOffset? CompletedAt,
        string? Reward,
        Guid? StackAfterId,
        string? StackAfterTitle,
        int? CurrentStreak,
        int? LongestStreak,
        bool MissedTwice,
        bool LastOccurrenceMissed,
        DateOnly? LastMissedDate,
        double? CompletionRate,
        IReadOnlyList<HabitDayResponse>? HabitDays,
        IReadOnlyList<AttachmentResponse> Attachments);

    public static async Task<Results<Ok<Response>, NotFound>> Handle(
        Guid id,
        DateOnly? date,
        LifeSchedulerDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var entry = await db.Schedules
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Date,
                e.Start,
                e.End,
                e.Notes,
                e.CreatedAt,
                e.Recurrence,
                e.Kind,
                e.CompletedAt,
                e.Reward,
                e.StackAfterId,
            })
            .FirstOrDefaultAsync(ct);

        if (entry is null)
            return TypedResults.NotFound();

        var attachments = await db.Set<ScheduleAttachment>()
            .AsNoTracking()
            .Where(a => a.ScheduleEntryId == id && a.Status == AttachmentStatus.Ready)
            .ToListAsync(ct);

        // Сортировка по DateTimeOffset в SQLite не транслируется — упорядочиваем в памяти.
        var attachmentResponses = attachments
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AttachmentResponse(
                a.Id, a.FileName, a.ContentType, a.SizeBytes, a.CreatedAt))
            .ToList();

        string? stackAfterTitle = null;
        int? currentStreak = null;
        int? longestStreak = null;
        var missedTwice = false;
        var lastOccurrenceMissed = false;
        DateOnly? lastMissedDate = null;
        double? completionRate = null;
        IReadOnlyList<HabitDayResponse>? habitDays = null;
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

        if (entry.Kind == ScheduleKind.Habit)
        {
            var completedDates = await db.OccurrenceCompletions
                .AsNoTracking()
                .Where(c => c.ScheduleEntryId == id)
                .Select(c => c.Date)
                .ToListAsync(ct);

            var completed = completedDates.ToHashSet();

            var streaks = HabitStatistics.Streaks(entry.Date, entry.Recurrence, completed, today);
            var misses = HabitStatistics.FindMisses(entry.Date, entry.Recurrence, completed, today);

            currentStreak = streaks.Current;
            longestStreak = streaks.Longest;
            missedTwice = misses.MissedTwice;
            lastOccurrenceMissed = misses.LastOccurrenceMissed;
            lastMissedDate = misses.LastMissedDate;
            completionRate = HabitStatistics.CompletionRate(
                entry.Date, entry.Recurrence, completed, today.AddDays(-29), today, today);

            var anchor = date ?? today;
            var weekStart = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
            habitDays = HabitStatistics.Window(
                    entry.Date, entry.Recurrence, completed, weekStart, weekStart.AddDays(6))
                .Select(day => new HabitDayResponse(day.Date, day.Scheduled, day.Completed))
                .ToList();

            if (entry.StackAfterId is { } stackId)
            {
                stackAfterTitle = await db.Schedules
                    .AsNoTracking()
                    .Where(e => e.Id == stackId)
                    .Select(e => e.Title)
                    .FirstOrDefaultAsync(ct);
            }
        }

        var response = new Response(
            entry.Id,
            entry.Title,
            entry.Date,
            today,
            entry.Start,
            entry.End,
            entry.Notes,
            entry.CreatedAt,
            entry.Recurrence,
            entry.Kind,
            entry.CompletedAt,
            entry.Reward,
            entry.StackAfterId,
            stackAfterTitle,
            currentStreak,
            longestStreak,
            missedTwice,
            lastOccurrenceMissed,
            lastMissedDate,
            completionRate,
            habitDays,
            attachmentResponses);

        return TypedResults.Ok(response);
    }
}
