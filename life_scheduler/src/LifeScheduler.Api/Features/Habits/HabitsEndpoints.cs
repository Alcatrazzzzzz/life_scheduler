namespace LifeScheduler.Api.Features.Habits;

public static class HabitsEndpoints
{
    public static IEndpointRouteBuilder MapHabitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/habits").WithTags("Habits");

        ListHabits.Map(group);

        return app;
    }
}
