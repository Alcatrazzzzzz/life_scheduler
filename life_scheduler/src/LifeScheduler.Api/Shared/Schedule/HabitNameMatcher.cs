using System.Text;

namespace LifeScheduler.Api.Shared.Schedule;

public sealed record HabitCandidate(Guid Id, string Title);

public sealed record HabitLookupResult(
    Guid? HabitId,
    string? Title,
    bool Ambiguous,
    IReadOnlyList<string> Suggestions);

public static class HabitNameMatcher
{
    private const int PrefixMinLength = 4;
    private const int AmbiguityGap = 100;

    public static HabitLookupResult Lookup(
        string spoken,
        IReadOnlyList<HabitCandidate> candidates)
    {
        var query = Normalize(spoken);

        if (query.Length == 0 || candidates.Count == 0)
        {
            return new HabitLookupResult(null, null, false, []);
        }

        var scored = candidates
            .Select(candidate => new ScoredCandidate(
                candidate,
                Score(query, Normalize(candidate.Title))))
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Candidate.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (scored.Count == 0)
        {
            return new HabitLookupResult(null, null, false, []);
        }

        var suggestions = scored
            .Take(3)
            .Select(item => item.Candidate.Title)
            .ToArray();

        var best = scored[0];

        if (scored.Count > 1 && best.Score - scored[1].Score < AmbiguityGap)
        {
            return new HabitLookupResult(null, null, true, suggestions);
        }

        return new HabitLookupResult(
            best.Candidate.Id,
            best.Candidate.Title,
            false,
            suggestions);
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lowered = value.Trim().ToLowerInvariant().Replace('ё', 'е');
        var builder = new StringBuilder(lowered.Length);
        var previousWasSpace = false;

        foreach (var symbol in lowered)
        {
            if (char.IsLetterOrDigit(symbol) || symbol == '-')
            {
                builder.Append(symbol);
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private static int Score(string query, string title)
    {
        if (title.Length == 0)
        {
            return 0;
        }

        if (title == query)
        {
            return 1000;
        }

        var best = ScorePair(title, query);
        var titleWords = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var titleWord in titleWords)
        {
            foreach (var queryWord in queryWords)
            {
                best = Math.Max(best, ScorePair(titleWord, queryWord));
            }
        }

        if (title.Contains(query, StringComparison.Ordinal))
        {
            best = Math.Max(best, 820);
        }

        return best;
    }

    private static int ScorePair(string candidate, string spoken)
    {
        if (candidate == spoken)
        {
            return 1000;
        }

        if (candidate.StartsWith(spoken, StringComparison.Ordinal))
        {
            return 900;
        }

        if (spoken.StartsWith(candidate, StringComparison.Ordinal))
        {
            return 850;
        }

        if (candidate.Contains(spoken, StringComparison.Ordinal))
        {
            return 800;
        }

        if (spoken.Contains(candidate, StringComparison.Ordinal))
        {
            return 780;
        }

        var best = 0;
        var prefix = CommonPrefixLength(candidate, spoken);

        if (prefix >= PrefixMinLength && Math.Abs(candidate.Length - spoken.Length) <= 2)
        {
            best = 850;
        }

        var distance = Levenshtein(candidate, spoken);
        var allowed = Math.Max(1, Math.Max(candidate.Length, spoken.Length) / 4);

        if (distance <= allowed)
        {
            best = Math.Max(best, 600 - (distance * 50));
        }

        return best;
    }

    private static int CommonPrefixLength(string left, string right)
    {
        var limit = Math.Min(left.Length, right.Length);
        var index = 0;

        while (index < limit && left[index] == right[index])
        {
            index++;
        }

        return index;
    }

    private static int Levenshtein(string left, string right)
    {
        if (left.Length == 0)
        {
            return right.Length;
        }

        if (right.Length == 0)
        {
            return left.Length;
        }

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var column = 0; column <= right.Length; column++)
        {
            previous[column] = column;
        }

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;

            for (var column = 1; column <= right.Length; column++)
            {
                var cost = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private sealed record ScoredCandidate(HabitCandidate Candidate, int Score);
}
