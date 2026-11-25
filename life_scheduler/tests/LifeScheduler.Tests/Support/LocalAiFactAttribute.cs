namespace LifeScheduler.Tests.Support;

public sealed class LocalAiFactAttribute : FactAttribute
{
    public LocalAiFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_LOCAL_AI_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Установите RUN_LOCAL_AI_TESTS=1 и положите модели в " +
                   "src/LifeScheduler.Api/models/.";
        }
    }
}
