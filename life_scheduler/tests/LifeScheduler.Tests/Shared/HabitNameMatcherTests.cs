using LifeScheduler.Api.Shared.Schedule;

namespace LifeScheduler.Tests.Shared;

public sealed class HabitNameMatcherTests
{
    [Fact]
    public void Lookup_FindsHabitInAccusative()
    {
        var habitId = Guid.NewGuid();

        var result = HabitNameMatcher.Lookup(
            "планёрку",
            [new HabitCandidate(habitId, "Планёрка")]);

        Assert.False(result.Ambiguous);
        Assert.Equal(habitId, result.HabitId);
        Assert.Equal("Планёрка", result.Title);
    }

    [Fact]
    public void Lookup_FindsHabitWhenPhraseContainsVerb()
    {
        var habitId = Guid.NewGuid();

        var result = HabitNameMatcher.Lookup(
            "отметь планёрку",
            [new HabitCandidate(habitId, "Планёрка")]);

        Assert.Equal(habitId, result.HabitId);
    }

    [Fact]
    public void Lookup_NormalizesYoAndCase()
    {
        var habitId = Guid.NewGuid();

        var result = HabitNameMatcher.Lookup(
            "ЗАРЯДКУ",
            [new HabitCandidate(habitId, "зарядка")]);

        Assert.Equal(habitId, result.HabitId);
    }

    [Fact]
    public void Lookup_MatchesWordPrefix()
    {
        var habitId = Guid.NewGuid();

        var result = HabitNameMatcher.Lookup(
            "читать",
            [new HabitCandidate(habitId, "Читать 20 минут")]);

        Assert.Equal(habitId, result.HabitId);
    }

    [Fact]
    public void Lookup_ReturnsAmbiguousWhenTwoHabitsMatchEqually()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var result = HabitNameMatcher.Lookup(
            "читать",
            [
                new HabitCandidate(first, "Читать"),
                new HabitCandidate(second, "Читать книгу"),
            ]);

        Assert.True(result.Ambiguous);
        Assert.Null(result.HabitId);
        Assert.Equal(2, result.Suggestions.Count);
    }

    [Fact]
    public void Lookup_ReturnsNothingForUnrelatedPhrase()
    {
        var result = HabitNameMatcher.Lookup(
            "совершенно другая фраза",
            [new HabitCandidate(Guid.NewGuid(), "Планёрка")]);

        Assert.Null(result.HabitId);
        Assert.False(result.Ambiguous);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void Lookup_HandlesEmptyCandidates()
    {
        var result = HabitNameMatcher.Lookup("планёрку", []);

        Assert.Null(result.HabitId);
        Assert.Empty(result.Suggestions);
    }
}
