// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.SiteFilter;

public class UserFilter
{
    public required string Type { get; set; }
    public required string Value { get; set; }
}

public class DefaultUserFilterService : IUserFilterService
{
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;

    public DefaultUserFilterService(
        IUserGetter userGetter,
        IPrivacySettingsGetter privacySettingsGetter
    )
    {
        _userGetter = userGetter;
        _privacySettingsGetter = privacySettingsGetter;
    }

    public bool ShouldExcludeByCategory(string userFilterValue, string linkCategory)
    {
        return string.Equals(userFilterValue, linkCategory, StringComparison.OrdinalIgnoreCase);
    }

    public bool ShouldExcludeByDomain(string userFilterValue, string linkDomain)
    {
        return !string.IsNullOrEmpty(linkDomain) && linkDomain.Contains(userFilterValue, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<long>> FilterUsersByLink(
        List<long> userIds,
        string linkUrl,
        ILinkCategorizer categorizer
        )
    {
        var filteredUsers = new List<long>();
        string linkCategory = categorizer.DetermineCategory(linkUrl);
        string linkDomain = CommonUtilities.ExtractDomain(linkUrl);

        foreach (long userId in userIds)
        {
            var userFilters = await GetUserFilters(userId);
            bool shouldExclude = false;

            foreach (var filter in userFilters)
            {
                if (filter.Type == "category" && 
                    ShouldExcludeByCategory(filter.Value, linkCategory))
                {
                    shouldExclude = true;
                    break;
                }

                if (filter.Type == "domain" && 
                    ShouldExcludeByDomain(filter.Value, linkDomain))
                {
                    shouldExclude = true;
                    break;
                }
            }

            if (!shouldExclude)
            {
                filteredUsers.Add(userId);
            }
        }

        return filteredUsers;
    }

    private async Task<List<UserFilter>> GetUserFilters(long chatId)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        if (userId <= 0) return new List<UserFilter>();

        var rules = await _privacySettingsGetter.GetAllActiveUserRulesWithTargets(userId);
        var filters = new List<UserFilter>();

        foreach (var rule in rules)
        {
            var filterType = rule.Type switch {
                PrivacyRuleType.SOCIAL_SITE_FILTER => "category",
                PrivacyRuleType.NSFW_SITE_FILTER => "category",
                PrivacyRuleType.UNIFIED_SITE_FILTER => "category",
                PrivacyRuleType.SITES_BY_DOMAIN_FILTER => "domain",
                _ => null
            };

            var filterValue = rule.Action switch {
                PrivacyRuleAction.SOCIAL_FILTER => "Social",
                PrivacyRuleAction.UNIFIED_FILTER => "UnifiedDomains",
                PrivacyRuleAction.NSFW_FILTER => "NSFW",
                PrivacyRuleAction.DOMAIN_FILTER => rule.TargetValue,
                _ => null
            };

            if (!string.IsNullOrEmpty(filterType) && !string.IsNullOrEmpty(filterValue))
            {
                filters.Add(new UserFilter {
                    Type = filterType,
                    Value = filterValue
                });
            }
        }

        return filters.Distinct().ToList();
    }
}