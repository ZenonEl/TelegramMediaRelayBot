// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.TelegramBot.SiteFilter;

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IPrivacySettingsSetter
{
    Task<bool> SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition);
    bool SetPrivacyRuleToDisabled(int userId, string type);
}

public interface IPrivacySettingsGetter
{
    bool GetIsActivePrivacyRule(int userId, string type);
    Task<int> GetPrivacyRuleId(int userId, string type);
    Task<string> GetPrivacyRuleValue(int userId, string type);
    Task<List<PrivacyRuleResult>> GetAllActiveUserRulesWithTargets(int userId);
}

