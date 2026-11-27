using System.Globalization;
using LifeScheduler.Api.Features.VoiceAssistant;

namespace LifeScheduler.Tests.Features.VoiceAssistant;

public sealed class RelativeTimeTests
{
    private static readonly DateTime Now = new(2026, 11, 27, 22, 13, 0, DateTimeKind.Unspecified);

    [Theory]
    [InlineData("пойти спать через час", "2026-11-27 23:13")]
    [InlineData("позвонить через полчаса", "2026-11-27 22:43")]
    [InlineData("позвонить через 20 минут", "2026-11-27 22:33")]
    [InlineData("позвонить через полтора часа", "2026-11-27 23:43")]
    [InlineData("позвонить через 2 часа", "2026-11-28 00:13")]
    [InlineData("позвонить через два часа", "2026-11-28 00:13")]
    public void TryResolve_RelativeExpressions(string text, string expected)
    {
        Assert.True(RelativeTime.TryResolve(text, Now, out var resolved));
        Assert.Equal(expected, resolved.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("встреча до обеда", 13, 0, true)]
    [InlineData("успеть к обеду", 13, 0, true)]
    [InlineData("позвони в обед", 13, 0, false)]
    [InlineData("встреча в полдень", 12, 0, false)]
    [InlineData("зарядка утром", 9, 0, false)]
    [InlineData("прогулка вечером", 19, 0, false)]
    public void TryResolveTimeOfDay_Phrases(string text, int hour, int minute, bool isEnd)
    {
        Assert.True(RelativeTime.TryResolveTimeOfDay(text, out var time, out var parsedIsEnd));
        Assert.Equal(new TimeOnly(hour, minute), time);
        Assert.Equal(isEnd, parsedIsEnd);
    }

    [Fact]
    public void TryResolveTimeOfDay_WithoutPhrase_ReturnsFalse()
    {
        Assert.False(RelativeTime.TryResolveTimeOfDay("встреча завтра в 15:00", out _, out _));
    }

    [Theory]
    [InlineData("отметь планёрку")]
    [InlineData("встреча завтра в 15:00")]
    [InlineData("какие у меня привычки")]
    public void TryResolve_WithoutRelative_ReturnsFalse(string text)
    {
        Assert.False(RelativeTime.TryResolve(text, Now, out _));
    }
}
