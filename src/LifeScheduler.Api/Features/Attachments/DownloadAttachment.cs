using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Attachments;

public static class DownloadAttachment
{
    public static async Task<Results<RedirectHttpResult, NotFound>> Handle(
        Guid id,
        Guid attachmentId,
        LifeSchedulerDbContext db,
        IFileStorage storage,
        CancellationToken ct)
    {
        var attachment = await db.Set<ScheduleAttachment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == attachmentId
                     && a.ScheduleEntryId == id
                     && a.Status == AttachmentStatus.Ready,
                ct);

        if (attachment is null)
            return TypedResults.NotFound();

        var ticket = await storage.CreateDownloadTicketAsync(
            attachment.Id,
            attachment.StorageKey,
            attachment.FileName,
            attachment.ContentType,
            ct);

        return TypedResults.Redirect(ticket.Url);
    }
}
