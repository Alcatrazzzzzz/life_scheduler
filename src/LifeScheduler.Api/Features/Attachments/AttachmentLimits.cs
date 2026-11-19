namespace LifeScheduler.Api.Features.Attachments;

public static class AttachmentLimits
{
    public const long MaxFileBytes = 25 * 1024 * 1024;
    public const int MaxFilesPerSchedule = 10;
    public const long MaxTotalBytesPerSchedule = 100 * 1024 * 1024;
}
