namespace LifeScheduler.Api.Shared.Data;

/// <summary>Отметка выполнения одного вхождения привычки — конкретного дня серии.</summary>
public sealed class ScheduleOccurrenceCompletion
{
    public Guid Id { get; init; }
    public Guid ScheduleEntryId { get; init; }
    public DateOnly Date { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
}
