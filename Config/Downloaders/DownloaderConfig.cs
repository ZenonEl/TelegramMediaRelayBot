namespace TelegramMediaRelayBot.Config.Downloaders;

// ===================================================================
// Корневой класс, который содержит всю конфигурацию
// ===================================================================
public class DownloaderConfigRoot
{
    public GlobalSettingsConfig GlobalSettings { get; set; } = new();
    public List<ProxyConfig> Proxies { get; set; } = new();
    public List<RetryPolicyConfig> RetryPolicies { get; set; } = new();
    public List<DownloaderDefinition> Downloaders { get; set; } = new();
}

// ===================================================================
// Глобальные Настройки
// ===================================================================
public class GlobalSettingsConfig
{
    public int MaxConcurrentDownloads { get; set; } = 1;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
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
public class DownloaderDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int Priority { get; set; }
    public string ExecutablePath { get; set; } = string.Empty;
    public UrlMatchingConfig UrlMatching { get; set; } = new();
    public DownloaderProxyPolicyConfig ProxyPolicy { get; set; } = new();
    public string ArgumentTemplate { get; set; } = string.Empty;
}

public class UrlMatchingConfig
{
    public UrlMatchingMode Mode { get; set; }
    public List<string> Patterns { get; set; } = new();
}

public enum UrlMatchingMode
{
    Patterns,
    Any
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