// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.SiteFilter;

public class HashTableLinkCategorizer : ILinkCategorizer
{
    private readonly HashSet<string> _socialDomains;
    private readonly HashSet<string> _nsfwDomains;
    private readonly HashSet<string> _unifiedDomains;
    private readonly IUrlParsingService _urlParsingService;

    public HashTableLinkCategorizer(IDomainsLoader domainsLoader, IUrlParsingService urlParser)
    {
        _socialDomains = domainsLoader.LoadDomainsFromFile("DomainLists/Social/hosts");
        _nsfwDomains = domainsLoader.LoadDomainsFromFile("DomainLists/NSFW/hosts");
        _unifiedDomains = domainsLoader.LoadDomainsFromFile("DomainLists/UnifiedDomains/hosts");
        _urlParsingService = urlParser;
    }

    public string DetermineCategory(string url)
    {
        _urlParsingService.TryExtractLinkAndText(url, out string domain, out string _);

        if (_socialDomains.Contains(domain)) return "Social";
        if (_nsfwDomains.Contains(domain)) return "NSFW";
        if (_unifiedDomains.Contains(domain)) return "UnifiedDomains";

        return "Unknown";
    }
}
