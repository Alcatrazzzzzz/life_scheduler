using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Schedule;

public static class ListSchedule
{
    public sealed record Response(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeOnly? Start,
        TimeOnly? End,
        string? Notes,
        DateTimeOffset CreatedAt,
        ScheduleRecurrence Recurrence,
        int AttachmentCount);

    public static async Task<Ok<IReadOnlyList<Response>>> Handle(
        DateOnly? from,
        DateOnly? to,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var query = db.Schedules.AsNoTracking();

        // Повторяющиеся записи отбираем по дате первого вхождения — дальше они разворачиваются в даты диапазона.
        if (from is not null)
            query = query.Where(e => e.Date >= from.Value || e.Recurrence != ScheduleRecurrence.None);

        if (to is not null)
            query = query.Where(e => e.Date <= to.Value || e.Recurrence != ScheduleRecurrence.None);

        var rows = await query
            .Select(e => new Response(
                e.Id,
                e.Title,
                e.Date,
                e.Start,
                e.End,
                e.Notes,
                e.CreatedAt,
                e.Recurrence,
                db.Set<ScheduleAttachment>()
                    .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready)))
            .ToListAsync(ct);

        IReadOnlyList<Response> items;
        if (from is null || to is null)
        {
            // Без диапазона повторы не разворачиваем: отдаём каждую запись одним вхождением.
            items = rows;
        }
        else
        {
            items = rows
                .SelectMany(row => OccurrenceDates(row.Date, row.Recurrence, from.Value, to.Value)
                    .Select(date => row with { Date = date }))
                .ToList();
        }

        return TypedResults.Ok<IReadOnlyList<Response>>(
            items.OrderBy(e => e.Date)
                .ThenBy(e => e.Start.HasValue ? 1 : 0)
                .ThenBy(e => e.Start)
                .ToList());
    }

    private static IEnumerable<DateOnly> OccurrenceDates(
        DateOnly seed,
        ScheduleRecurrence recurrence,
        DateOnly from,
        DateOnly to)
    {
        if (recurrence == ScheduleRecurrence.None)
        {
            if (seed >= from && seed <= to)
                yield return seed;

            yield break;
        }

        if (recurrence == ScheduleRecurrence.Daily)
        {
            for (var date = seed > from ? seed : from; date <= to; date = date.AddDays(1))
                yield return date;

            yield break;
        }

        // Weekly: шагаем от seed, но начинаем с первого вхождения не раньше from.
        var aligned = seed > from ? seed : from;
        var offset = ((int)seed.DayOfWeek - (int)aligned.DayOfWeek + 7) % 7;

        for (var date = aligned.AddDays(offset); date <= to; date = date.AddDays(7))
            yield return date;
    }
}
