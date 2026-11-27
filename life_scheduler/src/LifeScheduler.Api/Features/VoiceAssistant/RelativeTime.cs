using System.Globalization;
using System.Text.RegularExpressions;

namespace LifeScheduler.Api.Features.VoiceAssistant;

public static partial class RelativeTime
{
    private static readonly (string Phrase, TimeOnly Time, bool IsEnd)[] TimeOfDay =
    [
        ("до обеда", new TimeOnly(13, 0), true),
        ("к обеду", new TimeOnly(13, 0), true),
        ("в обед", new TimeOnly(13, 0), false),
        ("в полдень", new TimeOnly(12, 0), false),
        ("утром", new TimeOnly(9, 0), false),
        ("днём", new TimeOnly(14, 0), false),
        ("вечером", new TimeOnly(19, 0), false),
        ("ночью", new TimeOnly(23, 0), false),
        ("в полночь", new TimeOnly(0, 0), false),
    ];

    private static readonly Dictionary<string, double> Numerals = new(StringComparer.OrdinalIgnoreCase)
    {
        ["один"] = 1,
        ["одну"] = 1,
        ["два"] = 2,
        ["две"] = 2,
        ["три"] = 3,
        ["четыре"] = 4,
        ["пять"] = 5,
        ["шесть"] = 6,
        ["семь"] = 7,
        ["восемь"] = 8,
        ["девять"] = 9,
        ["десять"] = 10,
        ["одиннадцать"] = 11,
        ["двенадцать"] = 12,
        ["полтора"] = 1.5,
    };

    // «через час», «через полчаса», «через 20 минут», «через полтора часа».
    [GeneratedRegex(
        @"через\s+(?:(?<num>\d+(?:[.,]\d+)?|полтора|один|одну|два|две|три|четыре|пять|шесть|семь|восемь|девять|десять|одиннадцать|двенадцать)\s*)?(?<unit>пол\s*часа|минут\w*|мин\b|час\w*|ч\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Expression();

    public static bool TryResolve(string text, DateTime now, out DateTime resolved)
    {
        resolved = now;

        var match = Expression().Match(text);

        if (!match.Success)
        {
            return false;
        }

        var unit = match.Groups["unit"].Value;

        if (unit.StartsWith("пол", StringComparison.OrdinalIgnoreCase))
        {
            resolved = now.AddMinutes(30);
            return true;
        }

        var amount = 1d;

        if (match.Groups["num"].Success
            && !TryReadNumber(match.Groups["num"].Value, out amount))
        {
            return false;
        }

        var minutes = unit.StartsWith("мин", StringComparison.OrdinalIgnoreCase)
            ? amount
            : amount * 60;

        resolved = now.AddMinutes(minutes);
        return true;
    }

    public static bool TryResolveTimeOfDay(string text, out TimeOnly time, out bool isEnd)
    {
        foreach (var (phrase, value, end) in TimeOfDay)
        {
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                time = value;
                isEnd = end;
                return true;
            }
        }

        time = default;
        isEnd = false;
        return false;
    }

    private static bool TryReadNumber(string value, out double number)
    {
        var normalized = value.Replace(',', '.');

        if (double.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out number))
        {
            return true;
        }

        return Numerals.TryGetValue(value, out number);
    }
}
