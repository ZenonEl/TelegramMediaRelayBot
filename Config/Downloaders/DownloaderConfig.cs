// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Config.Downloaders;

// ===================================================================
// Корневой класс, который содержит всю конфигурацию
// ===================================================================
public class DownloaderConfigRoot
{
    public GlobalSettingsConfig GlobalSettings { get; set; } = new();
    public List<ProxyConfig> Proxies { get; set; } = new();
    public List<RetryPolicyConfig> RetryPolicies { get; set; } = new();
    public MediaProcessingConfig MediaProcessing { get; set; } = new();
    public List<DownloaderDefinition> Downloaders { get; set; } = new();
}

// ===================================================================
// Глобальные Настройки
// ===================================================================
public enum ProgressLogLevel { Normal, Verbose }

public class GlobalSettingsConfig
{
    public int MaxConcurrentDownloads { get; set; } = 1;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public ProgressLogLevel DownloadProgressLogLevel { get; set; } = ProgressLogLevel.Normal;
}

// ===================================================================
// Настройки Прокси-серверов
// ===================================================================
public class ProxyConfig
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Address { get; set; } = string.Empty;
    public int? ControlPort { get; set; }
    public string? ControlPassword { get; set; }
}

// ===================================================================
// Политики Повторных Попыток (Ретраи)
// ===================================================================
public class RetryPolicyConfig
{
    public string Name { get; set; } = string.Empty;
    public List<string> ErrorPatterns { get; set; } = new();
    public int MaxAttempts { get; set; } = 1;
    public TimeSpan Delay { get; set; }
    public RetryAction Action { get; set; }
    public string? ProxyName { get; set; }
}

public enum RetryAction
{
    None,
    UseProxy,
    ChangeProxyChain // Задел на будущее для управления Tor
}

// ===================================================================
// Определения Загрузчиков
// ===================================================================
public class AuthenticationConfig
{
    public string? CookieFile { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ApiToken { get; set; }
}

public class DownloaderDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int Priority { get; set; }
    public string ExecutablePath { get; set; } = string.Empty;
    public UrlMatchingConfig UrlMatching { get; set; } = new();
    public DownloaderProxyPolicyConfig ProxyPolicy { get; set; } = new();
    public AuthenticationConfig Authentication { get; set; } = new();
    public List<string> PostProcessors { get; set; } = new();
    public List<string> ArgumentList { get; set; } = new();
}

public class UrlMatchingConfig
{
    public UrlMatchingMode Mode { get; set; }
    public List<string> Patterns { get; set; } = new();
    public string? PatternsFile { get; set; }
}

public enum UrlMatchingMode
{
    Whitelist,
    Any,
    Blacklist
}

public class DownloaderProxyPolicyConfig
{
    /// <summary>
    /// Имя прокси по умолчанию. "false" или null/пусто означает не использовать прокси.
    /// </summary>
    public string? Default { get; set; }

    /// <summary>
    /// Правила для конкретных доменов. Ключ - домен (например, "tiktok.com").
    /// Значение - имя прокси, или "false"/null/пусто для отключения прокси.
    /// </summary>
    public Dictionary<string, string?> SiteSpecific { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class MediaProcessingConfig
{
    // Сколько одновременно работающих ffmpeg разрешено (Лимит семафора)
    public int MaxConcurrentProcessings { get; set; } = 2;

    // Настройки конкретно для конвертера
    public SplitterConfig Splitter { get; set; } = new();
    public FfmpegConfig Ffmpeg { get; set; } = new();
}

public class SplitterConfig
{
    public bool Enabled { get; set; } = true;

    // Порог в байтах. Если больше - режем.
    public long ThresholdBytes { get; set; } = 209715200;

    // Целевой размер куска.
    public long ChunkSizeBytes { get; set; } = 47185920;
}

public class FfmpegConfig
{
    // Путь к ffmpeg (если в PATH, то просто "ffmpeg")
    public string ExecutablePath { get; set; } = "ffmpeg";

    // Лимит времени на обработку (чтобы не висело вечно)
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(20);

    // Пресет скорости (ultrafast, superfast, veryfast, medium)
    // ultrafast = мгновенно, но файл большой. medium = долго, файл меньше.
    public string Preset { get; set; } = "veryfast";

    // CRF (Constant Rate Factor). 0-51.
    // 18 - визуально без потерь. 23 - стандарт. 28 - для моб. устройств.
    public int Crf { get; set; } = 26;
}
