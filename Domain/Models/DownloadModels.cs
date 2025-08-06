// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

namespace TelegramMediaRelayBot.Domain.Models;

[Flags]
public enum MediaType
{
    None = 0,
    Video = 1,
    Image = 2,
    Audio = 4,
    All = Video | Image | Audio
}

public class DownloadCapability
{
    public bool CanDownload { get; set; }
    public MediaType MediaTypes { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public bool RequiresAuth { get; set; }
    public bool RequiresProxy { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public long? EstimatedSize { get; set; }
}

public class DownloadResult
{
    public bool Success { get; set; }
    public List<byte[]> MediaFiles { get; set; } = new();
    public string? Caption { get; set; }
    public MediaType MediaType { get; set; }
    public TimeSpan Duration { get; set; }
    public long? FileSize { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DownloadOptions
{
    public MediaType PreferredMediaType { get; set; } = MediaType.All;
    public string? ProxyUrl { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    public int MaxRetries { get; set; } = 3;
    public Dictionary<string, object>? CustomSettings { get; set; }
    public ITelegramBotClient? BotClient { get; set; }
    public Message? StatusMessage { get; set; }
}

public class UserPreferences
{
    public int UserId { get; set; }
    public List<string> FrequentDomains { get; set; } = new();
    public Dictionary<string, int> DownloaderSuccessRates { get; set; } = new();
    public Dictionary<string, int> DownloaderUsageCounts { get; set; } = new();
    public Dictionary<string, MediaType> DomainMediaTypes { get; set; } = new();
    public Dictionary<string, TimeSpan> AverageDownloadTimes { get; set; } = new();
    public Dictionary<string, long> AverageFileSizes { get; set; } = new();
}

public class CommandResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
} 