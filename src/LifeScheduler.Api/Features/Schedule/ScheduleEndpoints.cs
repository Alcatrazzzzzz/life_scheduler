namespace LifeScheduler.Api.Features.Schedule;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules").WithTags("Schedule");

        group.MapPost("/", CreateSchedule.Handle);
        group.MapGet("/", ListSchedule.Handle);
        group.MapGet("/{id:guid}", GetSchedule.Handle);
        group.MapPut("/{id:guid}", UpdateSchedule.Handle);
        group.MapDelete("/{id:guid}", DeleteSchedule.Handle);
    }
}
