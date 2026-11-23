namespace LifeScheduler.Api.Shared.Schedule;

public static class PastEntryRules
{
    public static bool IsPast(DateOnly date, TimeOnly? start, TimeOnly? end, DateTime localNow)
    {
        var today = DateOnly.FromDateTime(localNow);

        if (date < today)
            return true;

        if (date > today || start is null)
            return false;

        return (end ?? start).Value <= TimeOnly.FromDateTime(localNow);
    }

    /// <summary>День закончился: задачи получают статус «не выполнена» только после полуночи.</summary>
    public static bool IsDayOver(DateOnly date, DateTime localNow) =>
        date < DateOnly.FromDateTime(localNow);
}
