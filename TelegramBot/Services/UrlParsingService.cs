// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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

        Match match = Regex.Match(message, @"https?://[^\s]+", RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        link = match.Value.TrimEnd('.', ',', ';', '!', '?', ')', ']');

        string textBefore = message.Substring(0, match.Index).Trim();
        string textAfter = (match.Index + match.Length < message.Length)
            ? message.Substring(match.Index + match.Length).Trim()
            : string.Empty;

        text = $"{textBefore} {textAfter}".Trim();

        return true;
    }

    public string ExtractDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return string.Empty;
        return uri.Host.ToLower();
    }
}
