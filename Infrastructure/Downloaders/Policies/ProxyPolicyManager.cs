using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

public class ProxyPolicyManager : IProxyPolicyManager
{
    private readonly IReadOnlyDictionary<string, ProxyConfig> _proxies;

    public ProxyPolicyManager(IOptionsMonitor<DownloaderConfigRoot> configMonitor)
    {
        // Создаем словарь для быстрого поиска прокси по имени
        _proxies = configMonitor.CurrentValue.Proxies
            .Where(p => p.Enabled)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        
        // TODO: Подписаться на configMonitor.OnChange для обновления словаря при Hot Reload
    }

    public string? GetProxyAddress(DownloaderDefinition downloaderConfig, string url)
    {
        var policy = downloaderConfig.ProxyPolicy;
        var uri = new Uri(url);
        string? proxyNameToUse = null;

        // 1. Проверяем, есть ли правило для конкретного сайта
        foreach (var siteRule in policy.SiteSpecific)
        {
            // Используем EndsWith для поддержки поддоменов (e.g., m.tiktok.com)
            if (uri.Host.EndsWith(siteRule.Key, StringComparison.OrdinalIgnoreCase))
            {
                proxyNameToUse = siteRule.Value;
                break;
            }
        }

        // 2. Если для сайта правила не найдено, используем правило по умолчанию
        if (proxyNameToUse == null)
        {
            proxyNameToUse = policy.Default;
        }

        // 3. Обрабатываем результат
        if (string.IsNullOrEmpty(proxyNameToUse) || proxyNameToUse.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            // Правило говорит "не использовать прокси"
            Log.Debug("Proxy policy for {Url}: No proxy.", url);
            return null;
        }

        // 4. Ищем прокси с таким именем в нашем словаре
        if (_proxies.TryGetValue(proxyNameToUse, out var proxyConfig))
        {
            Log.Debug("Proxy policy for {Url}: Using proxy '{ProxyName}'.", url, proxyNameToUse);
            return proxyConfig.Address;
        }

        // 5. Если прокси с таким именем не найден или выключен
        Log.Warning("Proxy '{ProxyName}' is defined in a policy, but not found or disabled in the global Proxies list.", proxyNameToUse);
        return null;
    }
}