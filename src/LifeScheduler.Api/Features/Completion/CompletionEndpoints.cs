namespace LifeScheduler.Api.Features.Completion;

public static class CompletionEndpoints
{
    public static void MapCompletionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules").WithTags("Completion");

        group.MapPost("/{id:guid}/completion", CompleteSchedule.Handle);
        group.MapDelete("/{id:guid}/completion", UncompleteSchedule.Handle);
    }
}
