namespace LifeScheduler.Api.Shared.Schedule;

/// <summary>Итог записи: выполненная задача или день привычки, пропущенная задача либо ничего.</summary>
public enum ScheduleResult
{
    None,
    Completed,
    Missed,
}
