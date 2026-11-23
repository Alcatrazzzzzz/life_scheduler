using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.History;

public static class ListHistory
{
    private const int MaxItems = 200;

    public sealed record Response(
        Guid Id,
        string Title,
        DateOnly Date,
        TimeOnly? Start,
        TimeOnly? End,
        string? Notes,
        DateTimeOffset CreatedAt,
        ScheduleRecurrence Recurrence,
        ScheduleKind Kind,
        ScheduleResult Result,
        int AttachmentCount);

    public static async Task<Results<Ok<IReadOnlyList<Response>>, ValidationProblem>> Handle(
        DateOnly? from,
        DateOnly? to,
        string? q,
        string? kind,
        string? result,
        bool? hasAttachments,
        LifeSchedulerDbContext db,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var kindFilter = ParseEnum<ScheduleKind>(kind, "kind", errors);
        var resultFilter = ParseEnum<ScheduleResult>(result, "result", errors);

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        // События и задачи живут строками в Schedules, отметки привычек — отдельными днями.
        var entries = await db.Schedules
            .AsNoTracking()
            .Where(e => e.Status == ScheduleStatus.Archived
                        && e.Recurrence == ScheduleRecurrence.None
                        && e.Kind != ScheduleKind.Habit)
            .Select(e => new Response(
                e.Id,
                e.Title,
                e.Date,
                e.Start,
                e.End,
                e.Notes,
                e.CreatedAt,
                e.Recurrence,
                e.Kind,
                e.Kind == ScheduleKind.Task
                    ? e.CompletedAt != null ? ScheduleResult.Completed : ScheduleResult.Missed
                    : ScheduleResult.None,
                db.Set<ScheduleAttachment>()
                    .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready)))
            .ToListAsync(ct);

        var habitMarks = await db.Set<ScheduleOccurrenceCompletion>()
            .AsNoTracking()
            .Join(
                db.Schedules.AsNoTracking().Where(e => e.Kind == ScheduleKind.Habit),
                c => c.ScheduleEntryId,
                e => e.Id,
                (c, e) => new Response(
                    e.Id,
                    e.Title,
                    c.Date,
                    e.Start,
                    e.End,
                    e.Notes,
                    e.CreatedAt,
                    e.Recurrence,
                    e.Kind,
                    ScheduleResult.Completed,
                    db.Set<ScheduleAttachment>()
                        .Count(a => a.ScheduleEntryId == e.Id && a.Status == AttachmentStatus.Ready)))
            .ToListAsync(ct);

        var items = entries
            .Concat(habitMarks)
            .Where(item => Matches(
                item, from, to, q, kindFilter, resultFilter, hasAttachments))
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Start)
            .Take(MaxItems)
            .ToList();

        return TypedResults.Ok<IReadOnlyList<Response>>(items);
    }

    private static TEnum? ParseEnum<TEnum>(
        string? value,
        string field,
        Dictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            return parsed;

        errors[field] = ["Неизвестное значение фильтра."];
        return null;
    }

    private static bool Matches(
        Response item,
        DateOnly? from,
        DateOnly? to,
        string? q,
        ScheduleKind? kind,
        ScheduleResult? result,
        bool? hasAttachments)
    {
        if (from is not null && item.Date < from.Value) return false;
        if (to is not null && item.Date > to.Value) return false;
        if (kind is not null && item.Kind != kind.Value) return false;
        if (result is not null && item.Result != result.Value) return false;
        if (hasAttachments is not null && (item.AttachmentCount > 0) != hasAttachments.Value) return false;

        // Поиск по кириллице регистронезависим только в памяти: SQLite lower()/LIKE
        // понижают регистр лишь у ASCII.
        var term = q?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var inTitle = item.Title.Contains(term, StringComparison.OrdinalIgnoreCase);
            var inNotes = item.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;

            if (!inTitle && !inNotes) return false;
        }

        return true;
    }
}
