// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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