// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Config.Services;

/// <summary>
/// Service for accessing configuration and access policy using IOptions pattern
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IOptionsMonitor<AccessPolicyConfiguration> _accessPolicyOptions;

    public ConfigurationService(IOptionsMonitor<AccessPolicyConfiguration> accessPolicyOptions)
    {
        _accessPolicyOptions = accessPolicyOptions;
    }

    /// <inheritdoc />
    public bool CanUserStartUsingBot(string referrerLink, IUserGetter userGetter)
    {
        AccessPolicyConfiguration accessPolicy = _accessPolicyOptions.CurrentValue;

        if (!accessPolicy.Enabled)
            return true;

        long referrerUserId = userGetter.GetUserTelegramIdByLink(referrerLink);
        if (referrerUserId == -1)
            return false;

        NewUsersPolicyConfiguration newUsersPolicy = accessPolicy.NewUsersPolicy;
        AllowRulesConfiguration allowRules = newUsersPolicy.AllowRules;

        bool isReferrerBlacklisted = allowRules.BlacklistedReferrerIds.Contains(referrerUserId);
        bool isReferrerWhitelisted = allowRules.WhitelistedReferrerIds.Contains(referrerUserId);

        return (allowRules.AllowAll && !isReferrerBlacklisted) ||
               (newUsersPolicy.Enabled && newUsersPolicy.AllowNewUsers && isReferrerWhitelisted);
    }
}
