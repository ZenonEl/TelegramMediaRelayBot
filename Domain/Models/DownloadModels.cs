// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

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
    public int AttemptNumber { get; set; }
    public NextAttemptModifiers? Modifiers { get; set; }
}

/// <summary>
/// Содержит опции для одной конкретной операции скачивания.
/// </summary>
public class DownloadOptions
{
    /// <summary>
    /// URL прокси-сервера, который нужно использовать для этой попытки.
    /// Может быть null, если прокси не нужен.
    /// </summary>
    public string? ProxyUrl { get; set; }

    /// <summary>
    /// Таймаут для этой конкретной операции.
    /// Nullable, чтобы мы могли понимать, было ли значение установлено.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Клиент Telegram, необходимый для обновления статуса.
    /// </summary>
    public ITelegramBotClient? BotClient { get; set; }

    /// <summary>
    /// Сообщение о статусе, которое нужно обновлять.
    /// </summary>
    public Message? StatusMessage { get; set; }

    /// <summary>
    /// Делегат, который будет вызываться для каждой строки лога от процесса.
    /// Позволяет "подписаться" на живые логи.
    /// </summary>
    public Action<string>? OnProgress { get; set; }

    /// <summary>
    /// Результат предыдущей неудачной попытки, если она была.
    /// </summary>
    public DownloadResult? LastResult { get; set; }
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