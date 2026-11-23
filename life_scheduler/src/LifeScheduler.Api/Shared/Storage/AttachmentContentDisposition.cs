namespace LifeScheduler.Api.Shared.Storage;

public static class AttachmentContentDisposition
{
    /// <summary>ASCII-fallback плюс RFC 5987 для кириллических имён.</summary>
    public static string ForAttachment(string fileName)
    {
        var fallback = new string(fileName
            .Select(character =>
                character is >= ' ' and <= '~' && character is not '"' and not '\\'
                    ? character
                    : '_')
            .ToArray());

        if (string.IsNullOrWhiteSpace(fallback))
            fallback = "file";

        return $"attachment; filename=\"{fallback}\"; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
    }
}
