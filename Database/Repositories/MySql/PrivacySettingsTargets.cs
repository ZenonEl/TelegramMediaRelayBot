// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Dapper;
using System.Data;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlPrivacySettingsTargetsSetter(IPrivacySettingsTargetsUoW uowService) : IPrivacySettingsTargetsSetter
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

public class MySqlPrivacySettingsTargetsGetter(IDbConnection dbConnection) : IPrivacySettingsTargetsGetter
{
    public async Task<bool> CheckPrivacyRuleTargetExists(int userId, string type)
    {
        const string query = @"
            SELECT EXISTS(
                SELECT 1 FROM PrivacySettingsTargets
                WHERE UserId = @userId AND TargetType = @type
                LIMIT 1
            )";


        var result = await dbConnection.ExecuteScalarAsync<bool>(query, new { userId, type });
        return result;
    }

    public async Task<List<string>> GetAllActiveUserRuleTargets(int userId)
    {
        const string query = @"
            SELECT TargetValue
            FROM PrivacySettingsTargets
            WHERE UserId = @userId";


        return (List<string>)await dbConnection.QueryAsync<List<string>>(query, new { userId });
    }
}
