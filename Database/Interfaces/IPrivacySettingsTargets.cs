// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;


public interface IPrivacySettingsTargetsSetter
{
    public Task<bool> SetPrivacyRuleTarget(int userId, int privacySettingId, string targetType, string targetValue);
    public Task<bool> SetToRemovePrivacyRuleTarget(int privacySettingId, string targetValue);
}

public interface IPrivacySettingsTargetsGetter
{
    public Task<bool> CheckPrivacyRuleTargetExists(int userId, string type);
    public Task<List<string>> GetAllActiveUserRuleTargets(int userId);
}