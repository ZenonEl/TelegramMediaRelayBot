using System.Text.RegularExpressions;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ICaptionFormatter
{
    string SanitizeRemoveHtml(string? input);
    string TrimToLimit(string? text);
}

public class CaptionFormatter : ICaptionFormatter
{
    private const int TelegramCaptionLimit = 1024;

    public string SanitizeRemoveHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        string noTags = Regex.Replace(input, "<[^>]+>", string.Empty);
        return noTags.Replace("\r", "").Replace("\u0000", "").Trim();
    }

    public string TrimToLimit(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= TelegramCaptionLimit ? text : text[..TelegramCaptionLimit];
    }
}