namespace LifeScheduler.Api.Features.History;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/schedules/history", ListHistory.Handle).WithTags("History");
    }
}
