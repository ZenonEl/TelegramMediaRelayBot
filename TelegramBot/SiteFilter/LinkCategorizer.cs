// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


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
