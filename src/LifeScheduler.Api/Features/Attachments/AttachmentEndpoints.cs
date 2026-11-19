namespace LifeScheduler.Api.Features.Attachments;

public static class AttachmentEndpoints
{
    public static void MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules/{id:guid}/attachments").WithTags("Attachments");

        group.MapPost("/", AddAttachment.Handle);
        group.MapPost("/{attachmentId:guid}/confirm", ConfirmAttachment.Handle);
        group.MapGet("/{attachmentId:guid}/download", DownloadAttachment.Handle);
        group.MapDelete("/{attachmentId:guid}", DeleteAttachment.Handle);
    }
}
