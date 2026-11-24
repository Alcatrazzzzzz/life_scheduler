using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.Schedule;

namespace LifeScheduler.Tests.Shared;

public sealed class HabitRulesTests
{
    [Fact]
    public void ValidateReward_AllowsEmptyRewardForAnyKind()
    {
        Assert.Empty(HabitRules.ValidateReward(ScheduleKind.Event, null));
        Assert.Empty(HabitRules.ValidateReward(ScheduleKind.Task, "  "));
    }

    [Fact]
    public void ValidateReward_RejectsRewardForNonHabit()
    {
        var errors = HabitRules.ValidateReward(ScheduleKind.Task, "Кофе");

        Assert.True(errors.ContainsKey("reward"));
    }

    [Fact]
    public void ValidateReward_RejectsTooLongReward()
    {
        var errors = HabitRules.ValidateReward(ScheduleKind.Habit, new string('я', 201));

        Assert.True(errors.ContainsKey("reward"));
    }

    [Fact]
    public void WouldCreateCycle_DetectsSelfReference()
    {
        var habitId = Guid.NewGuid();

        Assert.True(HabitRules.WouldCreateCycle(habitId, habitId, new Dictionary<Guid, Guid?>()));
    }

    [Fact]
    public void WouldCreateCycle_DetectsIndirectCycle()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var links = new Dictionary<Guid, Guid?> { [c] = b, [b] = a, [a] = null };

        Assert.True(HabitRules.WouldCreateCycle(a, c, links));
    }

    [Fact]
    public void WouldCreateCycle_AllowsLinearChain()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var links = new Dictionary<Guid, Guid?> { [a] = null };

        Assert.False(HabitRules.WouldCreateCycle(b, a, links));
    }

    [Fact]
    public void WouldCreateCycle_NullTargetIsAlwaysAllowed()
    {
        Assert.False(HabitRules.WouldCreateCycle(Guid.NewGuid(), null, new Dictionary<Guid, Guid?>()));
    }
}
