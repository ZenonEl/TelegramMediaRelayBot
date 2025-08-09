// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TelegramMediaRelayBot.Config.Services;

public class ConfigurationChangeLogger
{
    private readonly IOptionsMonitor<BotConfiguration> _botOptions;
    private readonly IOptionsMonitor<MessageDelayConfiguration> _delayOptions;
    private readonly IOptionsMonitor<LoggingConfiguration> _loggingOptions;
    private readonly IOptionsMonitor<TorConfiguration> _torOptions;
    private readonly IOptionsMonitor<DownloaderSettingsConfiguration> _downloaderOptions;
    private readonly IOptionsMonitor<DownloadingConfiguration> _downloadingOptions;

    private BotConfiguration _lastBot = new();
    private MessageDelayConfiguration _lastDelay = new();
    private LoggingConfiguration _lastLogging = new();
    private TorConfiguration _lastTor = new();
    private DownloaderSettingsConfiguration _lastDownloader = new();
    private DownloadingConfiguration _lastDownloading = new();

    public ConfigurationChangeLogger(
        IOptionsMonitor<BotConfiguration> botOptions,
        IOptionsMonitor<MessageDelayConfiguration> delayOptions,
        IOptionsMonitor<LoggingConfiguration> loggingOptions,
        IOptionsMonitor<TorConfiguration> torOptions,
        IOptionsMonitor<DownloaderSettingsConfiguration> downloaderOptions,
        IOptionsMonitor<DownloadingConfiguration> downloadingOptions)
    {
        _botOptions = botOptions;
        _delayOptions = delayOptions;
        _loggingOptions = loggingOptions;
        _torOptions = torOptions;
        _downloaderOptions = downloaderOptions;
        _downloadingOptions = downloadingOptions;

        // Initialize baselines
        _lastBot = DeepClone(_botOptions.CurrentValue);
        _lastDelay = DeepClone(_delayOptions.CurrentValue);
        _lastLogging = DeepClone(_loggingOptions.CurrentValue);
        _lastTor = DeepClone(_torOptions.CurrentValue);
        _lastDownloader = DeepClone(_downloaderOptions.CurrentValue);
        // Subscribe to changes immediately
        _botOptions.OnChange(newVal => LogChanges("AppSettings", ref _lastBot, newVal));
        _delayOptions.OnChange(newVal => LogChanges("MessageDelaySettings", ref _lastDelay, newVal));
        _loggingOptions.OnChange(newVal => LogChanges("ConsoleOutputSettings", ref _lastLogging, newVal));
        _torOptions.OnChange(newVal => LogChanges("Tor", ref _lastTor, newVal));
        _downloaderOptions.OnChange(newVal => LogChanges("AppSettings:DownloaderSettings", ref _lastDownloader, newVal));
        _downloadingOptions.OnChange(newVal => LogChanges("Downloading", ref _lastDownloading, newVal));
    }

    private static T DeepClone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    private static void LogChanges<T>(string sectionName, ref T lastValue, T newValue)
    {
        var changes = GetPropertyChanges(lastValue!, newValue!);
        if (changes.Count > 0)
        {
            foreach (var (prop, oldVal, newValStr) in changes)
            {
                Log.Information("Config changed [{Section}]: {Property}: '{Old}' -> '{New}'", sectionName, prop, oldVal, newValStr);
            }
            lastValue = DeepClone(newValue);
        }
    }

    private static List<(string Property, string OldValue, string NewValue)> GetPropertyChanges<T>(T oldObj, T newObj, string? prefix = null)
    {
        var list = new List<(string, string, string)>();
        if (oldObj == null || newObj == null) return list;

        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        foreach (var prop in props)
        {
            var oldVal = prop.GetValue(oldObj);
            var newVal = prop.GetValue(newObj);

            var name = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

            if (IsSimpleType(prop.PropertyType))
            {
                var oldStr = oldVal?.ToString() ?? string.Empty;
                var newStr = newVal?.ToString() ?? string.Empty;
                if (!string.Equals(oldStr, newStr, StringComparison.Ordinal))
                {
                    list.Add((name, oldStr, newStr));
                }
            }
            else if (oldVal != null && newVal != null)
            {
                // Recurse one level for complex nested objects
                var nested = typeof(ConfigurationChangeLogger)
                    .GetMethod(nameof(GetPropertyChanges), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(prop.PropertyType)
                    .Invoke(null, new object?[] { oldVal, newVal, name }) as List<(string, string, string)>;
                if (nested != null) list.AddRange(nested);
            }
        }

        return list;
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(TimeSpan);
    }
}

