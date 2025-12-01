// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;

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
    private readonly IUrlParsingService _urlParser;

    public DefaultUserFilterService(
        IUserGetter userGetter,
        IPrivacySettingsGetter privacySettingsGetter,
        IUrlParsingService urlParser
    )
    {
        _userGetter = userGetter;
        _privacySettingsGetter = privacySettingsGetter;
        _urlParser = urlParser;
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
        List<long> filteredUsers = new List<long>();
        string linkCategory = categorizer.DetermineCategory(linkUrl);
        _urlParser.TryExtractLinkAndText(linkUrl, out string linkDomain, out string _);

        foreach (long userId in userIds)
        {
            List<UserFilter> userFilters = await GetUserFilters(userId);
            bool shouldExclude = false;

            foreach (UserFilter filter in userFilters)
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

        List<PrivacyRuleResult> rules = await _privacySettingsGetter.GetAllActiveUserRulesWithTargets(userId);
        List<UserFilter> filters = new List<UserFilter>();

        foreach (PrivacyRuleResult rule in rules)
        {
            string? filterType = rule.Type switch
            {
                PrivacyRuleType.SOCIAL_SITE_FILTER => "category",
                PrivacyRuleType.NSFW_SITE_FILTER => "category",
                PrivacyRuleType.UNIFIED_SITE_FILTER => "category",
                PrivacyRuleType.SITES_BY_DOMAIN_FILTER => "domain",
                _ => null
            };

            string? filterValue = rule.Action switch
            {
                PrivacyRuleAction.SOCIAL_FILTER => "Social",
                PrivacyRuleAction.UNIFIED_FILTER => "UnifiedDomains",
                PrivacyRuleAction.NSFW_FILTER => "NSFW",
                PrivacyRuleAction.DOMAIN_FILTER => rule.TargetValue,
                _ => null
            };

            if (!string.IsNullOrEmpty(filterType) && !string.IsNullOrEmpty(filterValue))
            {
                filters.Add(new UserFilter
                {
                    Type = filterType,
                    Value = filterValue
                });
            }
        }

        return filters.Distinct().ToList();
    }
}
