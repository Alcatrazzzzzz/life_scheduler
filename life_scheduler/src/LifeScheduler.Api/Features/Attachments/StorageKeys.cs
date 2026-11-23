namespace LifeScheduler.Api.Features.Attachments;

public static class StorageKeys
{
    public static string ForAttachment(Guid scheduleId, Guid attachmentId) =>
        $"schedules/{scheduleId:N}/{attachmentId:N}";
}
