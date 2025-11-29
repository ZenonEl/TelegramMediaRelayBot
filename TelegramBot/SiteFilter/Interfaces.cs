// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.


namespace TelegramMediaRelayBot.TelegramBot.SiteFilter;

public interface ILinkCategorizer
{
    string DetermineCategory(string url);
}

public interface IUserFilterService
{
    bool ShouldExcludeByCategory(string userFilterValue, string linkCategory);
    bool ShouldExcludeByDomain(string userFilterValue, string linkDomain);
    Task<List<long>> FilterUsersByLink(
        List<long> userIds,
        string linkUrl,
        ILinkCategorizer categorizer
        );
}