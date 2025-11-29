// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.UnitOfWork.Services;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqlitePrivacySettingsTargetsSetter(PrivacySettingsTargetsUoWService uowService) : IPrivacySettingsTargetsSetter
{
    public Task<bool> SetPrivacyRuleTarget(int userId, int privacySettingId, string targetType, string targetValue)
    {
        return uowService.SetPrivacyRuleTarget(userId, privacySettingId, targetType, targetValue);
    }


    public Task<bool> SetToRemovePrivacyRuleTarget(int privacySettingId, string targetValue)
    {
        return uowService.SetToRemovePrivacyRuleTarget(privacySettingId, targetValue);
    }
}

public class SqlitePrivacySettingsTargetsGetter(IPrivacySettingsTargetsRepository repository) : IPrivacySettingsTargetsGetter
{
    public Task<bool> CheckPrivacyRuleTargetExists(int userId, string type)
    {
        return repository.CheckTargetExists(userId, type);
    }

    public Task<List<string>> GetAllActiveUserRuleTargets(int userId)
    {
        return repository.GetAllUserTargets(userId);
    }
}