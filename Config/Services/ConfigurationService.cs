// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Microsoft.Extensions.Options;
using System.Resources;
using TelegramMediaRelayBot.Config;
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
        var accessPolicy = _accessPolicyOptions.CurrentValue;
        
        if (!accessPolicy.Enabled) 
            return true;

        long referrerUserId = userGetter.GetUserTelegramIdByLink(referrerLink);
        if (referrerUserId == -1) 
            return false;

        var newUsersPolicy = accessPolicy.NewUsersPolicy;
        var allowRules = newUsersPolicy.AllowRules;

        bool isReferrerBlacklisted = allowRules.BlacklistedReferrerIds.Contains(referrerUserId);
        bool isReferrerWhitelisted = allowRules.WhitelistedReferrerIds.Contains(referrerUserId);

        return (allowRules.AllowAll && !isReferrerBlacklisted) ||
               (newUsersPolicy.Enabled && newUsersPolicy.AllowNewUsers && isReferrerWhitelisted);
    }
}
