using LifeScheduler.Api.Shared.Data;

namespace LifeScheduler.Api.Shared.Schedule;

/// <summary>Правила полей привычки и habit stacking.</summary>
public static class HabitRules
{
    public const int MaxRewardLength = 200;

    public static Dictionary<string, string[]> ValidateReward(ScheduleKind kind, string? reward)
    {
        var errors = new Dictionary<string, string[]>();

        if (kind != ScheduleKind.Habit && !string.IsNullOrWhiteSpace(reward))
            errors[nameof(reward)] = ["Награда доступна только привычке."];

        if (reward?.Trim().Length > MaxRewardLength)
            errors[nameof(reward)] = [$"Награда не длиннее {MaxRewardLength} символов."];

        return errors;
    }

    public static void ValidateStackTarget(
        ScheduleKind kind,
        Guid? stackAfterId,
        ScheduleEntry? target,
        Dictionary<string, string[]> errors)
    {
        if (stackAfterId is null)
            return;

        if (kind != ScheduleKind.Habit)
        {
            errors[nameof(stackAfterId)] = ["Цепочка доступна только привычке."];
            return;
        }

        if (target is null || target.Kind != ScheduleKind.Habit || target.Status != ScheduleStatus.Active)
            errors[nameof(stackAfterId)] = ["Привычка-триггер не найдена."];
    }

    public static string? NormalizeReward(string? reward) =>
        string.IsNullOrWhiteSpace(reward) ? null : reward.Trim();

    public static Guid? NormalizeStackAfterId(ScheduleKind kind, Guid? stackAfterId) =>
        kind == ScheduleKind.Habit ? stackAfterId : null;

    /// <summary>
    /// Проверяет, что предложенная связь stacking не создаёт цикл.
    /// <paramref name="links"/> — снимок «привычка → её stackAfter».
    /// </summary>
    public static bool WouldCreateCycle(
        Guid habitId,
        Guid? stackAfterId,
        IReadOnlyDictionary<Guid, Guid?> links)
    {
        if (stackAfterId is null)
            return false;

        var seen = new HashSet<Guid>();
        Guid? cursor = stackAfterId;

        while (cursor is { } current)
        {
            if (current == habitId || !seen.Add(current))
                return true;

            cursor = links.TryGetValue(current, out var next) ? next : null;
        }

        return false;
    }
}
