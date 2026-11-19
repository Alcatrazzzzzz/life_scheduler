namespace LifeScheduler.Api.Shared.Data;

public enum ScheduleRecurrence
{
    None,
    Daily,
    Weekly,
}

public enum ScheduleStatus
{
    Active,
    Archived,
}

public sealed class ScheduleEntry
{
    public Guid Id { get; init; }
    public required string Title { get; set; }
    // для повторяющейся записи это дата первого вхождения (seed), остальные считаются от неё
    public DateOnly Date { get; set; }
    // null Start/End означает «дело на день без конкретного времени», оба поля либо заданы, либо пусты
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public string? Notes { get; set; }
    public ScheduleRecurrence Recurrence { get; set; }
    // Archived — прошедшая неповторяющаяся запись, переведённая в историю архиватором
    public ScheduleStatus Status { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
