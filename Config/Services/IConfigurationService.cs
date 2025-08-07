// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

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
