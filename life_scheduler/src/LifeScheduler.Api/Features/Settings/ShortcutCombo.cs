namespace LifeScheduler.Api.Features.Settings;

public static class ShortcutCombo
{
    private static readonly string[] ModifierOrder = ["Ctrl", "Alt", "Shift", "Meta"];

    private static readonly Dictionary<string, string> ModifierAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = "Ctrl",
        ["control"] = "Ctrl",
        ["alt"] = "Alt",
        ["shift"] = "Shift",
        ["meta"] = "Meta",
        ["win"] = "Meta",
        ["cmd"] = "Meta",
        ["super"] = "Meta",
    };

    private static readonly Dictionary<string, string> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["space"] = "Space",
        ["enter"] = "Enter",
        ["escape"] = "Escape",
        ["esc"] = "Escape",
        ["tab"] = "Tab",
        ["backspace"] = "Backspace",
        ["delete"] = "Delete",
        ["insert"] = "Insert",
        ["home"] = "Home",
        ["end"] = "End",
        ["pageup"] = "PageUp",
        ["pagedown"] = "PageDown",
        ["arrowup"] = "ArrowUp",
        ["arrowdown"] = "ArrowDown",
        ["arrowleft"] = "ArrowLeft",
        ["arrowright"] = "ArrowRight",
    };

    public static bool TryNormalize(string? raw, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Укажите комбинацию клавиш.";
            return false;
        }

        var tokens = raw.Split('+', StringSplitOptions.TrimEntries);

        if (tokens.Any(string.IsNullOrEmpty))
        {
            error = "Комбинация содержит пустой элемент.";
            return false;
        }

        var modifiers = new HashSet<string>(StringComparer.Ordinal);
        string? key = null;

        foreach (var token in tokens)
        {
            if (ModifierAliases.TryGetValue(token, out var modifier))
            {
                if (key is not null)
                {
                    error = "Клавиша должна идти последней.";
                    return false;
                }

                modifiers.Add(modifier);
                continue;
            }

            if (key is not null)
            {
                error = "В комбинации может быть только одна клавиша.";
                return false;
            }

            key = NormalizeKey(token);
        }

        if (modifiers.Count == 0)
        {
            error = "Добавьте хотя бы один модификатор: Ctrl, Alt, Shift или Meta.";
            return false;
        }

        if (key is null)
        {
            error = "Добавьте клавишу к модификаторам.";
            return false;
        }

        normalized = string.Join('+', ModifierOrder.Where(modifiers.Contains).Append(key));
        return true;
    }

    private static string NormalizeKey(string token)
    {
        if (NamedKeys.TryGetValue(token, out var named))
            return named;

        return token.Length == 1 ? token.ToUpperInvariant() : token;
    }
}
