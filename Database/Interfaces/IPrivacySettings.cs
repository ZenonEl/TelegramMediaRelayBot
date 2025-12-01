// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IPrivacySettingsSetter
{
    Task<bool> SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition);
    Task<bool> SetPrivacyRuleToDisabled(int userId, string type);
}

public interface IPrivacySettingsGetter
{
    bool GetIsActivePrivacyRule(int userId, string type);
    Task<int> GetPrivacyRuleId(int userId, string type);
    Task<string> GetPrivacyRuleValue(int userId, string type);
    Task<List<PrivacyRuleResult>> GetAllActiveUserRulesWithTargets(int userId);
}

