using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Features.Attachments;

/// <summary>Подмена presigned-URL для локального хранилища в dev.</summary>
public static class LocalStorageEndpoints
{
    public static void MapLocalStorageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/storage/uploads/{attachmentId:guid}", Upload);
        app.MapGet("/api/storage/download/{attachmentId:guid}", Download);
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Upload(
        Guid attachmentId,
        HttpContext context,
        LifeSchedulerDbContext db,
        LocalFileStorage storage,
        CancellationToken ct)
    {
        var attachment = await db.Set<ScheduleAttachment>()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        if (attachment is null)
            return TypedResults.NotFound();

        if (attachment.Status != AttachmentStatus.Pending)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Файл уже подтверждён.");

        var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
            sizeFeature.MaxRequestBodySize = AttachmentLimits.MaxFileBytes;

        if (context.Request.ContentLength is > AttachmentLimits.MaxFileBytes)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Файл больше 25 МБ.");

        await storage.WriteAsync(attachment.StorageKey, context.Request.Body, ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> Download(
        Guid attachmentId,
        LifeSchedulerDbContext db,
        LocalFileStorage storage,
        CancellationToken ct)
    {
        var attachment = await db.Set<ScheduleAttachment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Status == AttachmentStatus.Ready, ct);

        if (attachment is null)
            return TypedResults.NotFound();

        try
        {
            var stream = storage.OpenRead(attachment.StorageKey);
            return TypedResults.File(
                stream,
                attachment.ContentType,
                attachment.FileName,
                enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
