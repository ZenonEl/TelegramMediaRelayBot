// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.TelegramBot.Utils;

public interface ITextCleanupService
{
    string Cleanup(string text, string? domainOrHost = null);
}

public class TextCleanupService : ITextCleanupService
{
    private readonly IOptionsMonitor<TextCleanupConfiguration> _options;

    public TextCleanupService(IOptionsMonitor<TextCleanupConfiguration> options)
    {
        _options = options;
    }

    public string Cleanup(string text, string? domainOrHost = null)
    {
        var cfg = _options.CurrentValue;
        if (!cfg.Enabled || string.IsNullOrEmpty(text)) return text;

        foreach (var rule in cfg.Rules)
        {
            if (rule == null || string.IsNullOrEmpty(rule.Pattern)) continue;
            if (rule.Domains != null && rule.Domains.Count > 0)
            {
                if (string.IsNullOrEmpty(domainOrHost)) continue;
                if (!rule.Domains.Any(d => string.Equals(d, domainOrHost, StringComparison.OrdinalIgnoreCase))) continue;
            }
            try
            {
                text = Regex.Replace(text, rule.Pattern, rule.Replacement ?? string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            catch { }
        }

        return text.Trim();
    }
}

