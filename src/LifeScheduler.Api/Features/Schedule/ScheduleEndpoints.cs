namespace LifeScheduler.Api.Features.Schedule;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules").WithTags("Schedule");

        group.MapPost("/", CreateSchedule.Handle);
        group.MapGet("/", ListSchedule.Handle);
    }
}
