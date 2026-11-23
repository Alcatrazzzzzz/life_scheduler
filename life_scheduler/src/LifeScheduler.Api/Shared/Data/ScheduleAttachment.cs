namespace LifeScheduler.Api.Shared.Data;

public enum AttachmentStatus
{
    Pending,
    Ready,
}

public sealed class ScheduleAttachment
{
    public Guid Id { get; init; }
    public Guid ScheduleEntryId { get; init; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    // заявленный при создании размер; при confirm заменяется фактическим из хранилища
    public long SizeBytes { get; set; }
    public required string StorageKey { get; set; }
    public AttachmentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

