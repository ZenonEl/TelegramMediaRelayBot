using System.Text.RegularExpressions;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IUrlParsingService
{
    bool IsLink(string input);
    bool TryExtractLinkAndText(string message, out string link, out string text);
    string ExtractDomain(string url);
}

public class UrlParsingService : IUrlParsingService
{
    public bool IsLink(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return Uri.TryCreate(input, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public bool TryExtractLinkAndText(string message, out string link, out string text)
    {
        link = string.Empty;
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(message)) return false;

        var m = Regex.Match(message, @"https?://[^\s]+", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            link = m.Value.TrimEnd('.', ',', ';', '!', '?', ')', ']');
            int startAfterUrl = m.Index + m.Length;
            text = startAfterUrl < message.Length ? message[startAfterUrl..].Trim() : string.Empty;
            return true;
        }
        return false;
    }

    public string ExtractDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return string.Empty;
        return uri.Host.ToLower();
    }
}