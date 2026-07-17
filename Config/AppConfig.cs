// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Serilog.Events;

namespace TelegramMediaRelayBot;

/// <summary>
/// Strongly-typed application configuration, bound from a single config file.
/// Validated once at startup (see <see cref="Validate"/>); a failure aborts the
/// bot with a clear list of problems instead of a NullReference deep in runtime.
/// </summary>
public sealed class AppConfig
{
    public BotOptions Bot { get; set; } = new();
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>Named proxies (name → URL), referenced by name everywhere else.</summary>
    public Dictionary<string, string> Proxies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public DownloadOptions Download { get; set; } = new();
    public TorOptions Tor { get; set; } = new();
    public SessionOptions Session { get; set; } = new();
    public DelayOptions Delays { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public AccessOptions Access { get; set; } = new();

    /// <summary>Resolves a proxy name to its URL. Null/empty name means "no proxy" (direct).</summary>
    public string? ResolveProxyUrl(string? proxyName)
    {
        if (string.IsNullOrWhiteSpace(proxyName)) return null;
        return Proxies.TryGetValue(proxyName, out var url) ? url : null;
    }

    /// <summary>
    /// Picks the download proxy URL for a given host: first matching rule wins,
    /// otherwise the default proxy. Returns null for a direct connection.
    /// </summary>
    public string? ResolveDownloadProxyUrl(string host)
    {
        foreach (var rule in Download.Rules)
        {
            if (rule.Matches(host))
                return ResolveProxyUrl(rule.Proxy);
        }
        return ResolveProxyUrl(Download.DefaultProxy);
    }

    /// <summary>Returns human-readable problems; empty means the config is valid.</summary>
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Bot.Token))
            yield return "Bot.Token is required.";
        if (string.IsNullOrWhiteSpace(Database.ConnectionString))
            yield return "Database.ConnectionString is required.";
        if (Download.MaxConcurrent < 1 || Download.MaxConcurrent > 20)
            yield return $"Download.MaxConcurrent must be between 1 and 20 (got {Download.MaxConcurrent}).";

        foreach (var problem in ValidateProxyReference("Bot.Proxy", Bot.Proxy))
            yield return problem;
        foreach (var problem in ValidateProxyReference("Download.DefaultProxy", Download.DefaultProxy))
            yield return problem;
        for (int i = 0; i < Download.Rules.Count; i++)
        {
            var rule = Download.Rules[i];
            if (rule.Hosts.Count == 0)
                yield return $"Download.Rules[{i}] has no Hosts.";
            foreach (var problem in ValidateProxyReference($"Download.Rules[{i}].Proxy", rule.Proxy))
                yield return problem;
        }
    }

    private IEnumerable<string> ValidateProxyReference(string field, string? proxyName)
    {
        if (!string.IsNullOrWhiteSpace(proxyName) && !Proxies.ContainsKey(proxyName))
            yield return $"{field} references proxy '{proxyName}' which is not defined in Proxies.";
    }
}

public sealed class BotOptions
{
    /// <summary>Telegram bot token from @BotFather.</summary>
    public string Token { get; set; } = "";
    /// <summary>Local Bot API server URL (e.g. http://telegram-bot-api:8081); null = official cloud API.</summary>
    public string? ApiBaseUrl { get; set; }
    /// <summary>Named proxy for the Telegram connection; null = direct.</summary>
    public string? Proxy { get; set; }
    public string Language { get; set; } = "en-US";
}

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = "Data Source=TelegramMediaRelayBot.db";
    public string Name { get; set; } = "TelegramMediaRelayBot";
}

public sealed class DownloadOptions
{
    public bool UseGalleryDl { get; set; } = true;
    public int MaxConcurrent { get; set; } = 3;
    /// <summary>Named proxy used when no rule matches; null = direct.</summary>
    public string? DefaultProxy { get; set; }
    /// <summary>yt-dlp cookies-from-browser spec, e.g. "firefox" or "firefox:~/.mozilla".</summary>
    public string? CookiesFromBrowser { get; set; }
    /// <summary>Path to a Netscape cookies.txt file.</summary>
    public string? CookiesFile { get; set; }
    /// <summary>
    /// Where downloads are stored; null = system temp. With a local Bot API server
    /// point this at the volume shared with it so files are sent by path, not upload.
    /// </summary>
    public string? TempDir { get; set; }
    /// <summary>Per-host proxy overrides; first match wins.</summary>
    public List<ProxyRule> Rules { get; set; } = new();
}

public sealed class ProxyRule
{
    /// <summary>Host patterns: exact ("vk.com") or suffix wildcard ("*.vk.com").</summary>
    public List<string> Hosts { get; set; } = new();
    /// <summary>Named proxy to use for these hosts; null = direct connection.</summary>
    public string? Proxy { get; set; }

    public bool Matches(string host)
    {
        foreach (var pattern in Hosts)
        {
            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                var suffix = pattern[1..]; // ".vk.com"
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                    host.Equals(pattern[2..], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (host.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class TorOptions
{
    public bool Enabled { get; set; }
    public string SocksHost { get; set; } = "127.0.0.1";
    public int SocksPort { get; set; } = 9050;
}

public sealed class SessionOptions
{
    public int TtlMinutes { get; set; } = 30;
    public int CleanupIntervalMinutes { get; set; } = 5;
    public int UnMuteCheckIntervalSeconds { get; set; } = 20;
}

public sealed class DelayOptions
{
    public int VideoGetMs { get; set; } = 1000;
    public int ContactSendMs { get; set; } = 1000;
}

public sealed class LoggingOptions
{
    public LogEventLevel Level { get; set; } = LogEventLevel.Information;
    public bool ShowDownloadProgress { get; set; }
    public bool ShowUploadProgress { get; set; }
}

public sealed class AccessOptions
{
    public bool Enabled { get; set; }
    public bool ShowDeniedMessage { get; set; }
    public string DeniedMessageContact { get; set; } = "";
    public bool AllowNewUsers { get; set; } = true;
    public bool AllowAll { get; set; } = true;
    public List<long> WhitelistedReferrerIds { get; set; } = new();
    public List<long> BlacklistedReferrerIds { get; set; } = new();
}
