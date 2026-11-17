namespace LifeScheduler.Api.Shared.Data;

public sealed class ScheduleEntry
{
    public Guid Id { get; init; }
    public required string Title { get; set; }
    public DateOnly Date { get; set; }
    // null Start/End означает «дело на день без конкретного времени», оба поля либо заданы, либо пусты
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
