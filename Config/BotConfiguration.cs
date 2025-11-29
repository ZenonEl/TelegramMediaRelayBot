// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

namespace TelegramMediaRelayBot.Config;

/// <summary>
/// Configuration for core bot settings
/// </summary>
public class BotConfiguration
{
    public string TelegramBotToken { get; set; } = string.Empty;
    public string SqlConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "TelegramMediaRelayBot";
    public string DatabaseType { get; set; } = "sqlite";
    public string? Language { get; set; }
    public string Proxy { get; set; } = string.Empty;
    public bool UseGalleryDl { get; set; } = false;
    public string AccessDeniedMessageContact { get; set; } = " ";
    public DownloaderSettingsConfiguration DownloaderSettings { get; set; } = new();
}

/// <summary>
/// Configuration for message delays and timing
/// </summary>
public class MessageDelayConfiguration
{
    public int VideoGetDelay { get; set; } = 1000;
    public int ContactSendDelay { get; set; } = 1000;
    public int UserUnMuteCheckInterval { get; set; } = 20; // Seconds
}

/// <summary>
/// Configuration for logging and console output
/// </summary>
public class LoggingConfiguration
{
    public Serilog.Events.LogEventLevel LogLevel { get; set; } = Serilog.Events.LogEventLevel.Information;
    public bool ShowVideoDownloadProgress { get; set; } = false;
    public bool ShowVideoUploadProgress { get; set; } = false;
    public bool EnableFileLogging { get; set; } = false;
    public string FilePath { get; set; } = "logs/bot-.log"; // rolling per day
    public long FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int RetainedFileCountLimit { get; set; } = 7;
}

/// <summary>
/// Configuration for Tor proxy settings
/// </summary>
public class TorConfiguration
{
    public bool Enabled { get; set; } = false;
    public string? TorControlPassword { get; set; }
    public string TorSocksHost { get; set; } = "127.0.0.1";
    public int TorSocksPort { get; set; } = 9050;
    public int TorControlPort { get; set; } = 9051;
    public int TorChangingChainInterval { get; set; } = 5; // Minutes
}

/// <summary>
/// Configuration for access policy and user restrictions
/// </summary>
public class AccessPolicyConfiguration
{
    public bool Enabled { get; set; } = false;
    public NewUsersPolicyConfiguration NewUsersPolicy { get; set; } = new();
}

/// <summary>
/// Configuration for new users policy
/// </summary>
public class NewUsersPolicyConfiguration
{
    public bool Enabled { get; set; } = false;
    public bool ShowAccessDeniedMessage { get; set; } = false;
    public bool AllowNewUsers { get; set; } = true;
    public AllowRulesConfiguration AllowRules { get; set; } = new();
}

/// <summary>
/// Configuration for allow rules with whitelisted and blacklisted users
/// </summary>
public class AllowRulesConfiguration
{
    public bool AllowAll { get; set; } = true;
    public List<long> WhitelistedReferrerIds { get; set; } = new();
    public List<long> BlacklistedReferrerIds { get; set; } = new();
}

/// <summary>
/// Configuration for downloader settings
/// </summary>
public class DownloaderSettingsConfiguration
{
    public string ConfigFilePath { get; set; } = "./downloader-config.yaml";
    public bool AutoLearningEnabled { get; set; } = true;
    public int MaxFrequentDomains { get; set; } = 20;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(30);
}
