// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlPrivacySettingsSetter(IPrivacySettingsUoW uowService) : IPrivacySettingsSetter
{
    public Task<bool> SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        // Делегируем вызов общему сервису
        return uowService.SetPrivacyRule(userId, type, action, isActive, actionCondition);
    }

    public Task<bool> SetPrivacyRuleToDisabled(int userId, string type)
    {
        // Делегируем вызов общему сервису
        return uowService.SetPrivacyRuleToDisabled(userId, type);
    }
}

public class MySqlPrivacySettingsGetter(IDbConnection dbConnection) : IPrivacySettingsGetter
{
    public bool GetIsActivePrivacyRule(int userId, string type)
    {
        const string query = @"
            SELECT IsActive
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        return dbConnection.ExecuteScalar<bool>(query, new {userId, type});
    }

    public async Task<int> GetPrivacyRuleId(int userId, string type)
    {
        const string query = @"
            SELECT ID
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        return await dbConnection.ExecuteScalarAsync<int>(query, new {userId, type});
    }

    public async Task<string> GetPrivacyRuleValue(int userId, string type)
    {
        const string query = @"
            SELECT Action
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        return await dbConnection.ExecuteScalarAsync<string>(query, new {userId, type}) ?? string.Empty;
    }

    public async Task<List<PrivacyRuleResult>> GetAllActiveUserRulesWithTargets(int userId)
    {
        const string query = @"
            SELECT
                PS.Type,
                PS.Action,
                PST.TargetValue
            FROM PrivacySettings PS
            LEFT JOIN PrivacySettingsTargets PST ON PS.Id = PST.PrivacySettingId
            WHERE PS.UserId = @userId
                AND PS.IsActive = 1
                AND PS.Action IN @actions";

        var allowedActions = new[] {
            PrivacyRuleAction.SOCIAL_FILTER,
            PrivacyRuleAction.NSFW_FILTER,
            PrivacyRuleAction.UNIFIED_FILTER,
            PrivacyRuleAction.DOMAIN_FILTER
        };

        var result = await dbConnection.QueryAsync<PrivacyRuleResult>(query, new {
            userId,
            actions = allowedActions
        });

        return result.ToList();
    }
}

