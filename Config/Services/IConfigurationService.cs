// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Config.Services;

/// <summary>
/// Service for accessing configuration and access policy
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Determines if a user can start using the bot based on access policy
    /// </summary>
    /// <param name="referrerLink">The referrer link used to access the bot</param>
    /// <param name="userGetter">Service to get user information</param>
    /// <returns>True if user can start using the bot</returns>
    bool CanUserStartUsingBot(string referrerLink, IUserGetter userGetter);
}
