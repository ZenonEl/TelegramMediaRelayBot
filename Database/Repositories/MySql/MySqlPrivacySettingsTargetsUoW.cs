// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlPrivacySettingsTargetsRepository(IDbConnection dbConnection) : IPrivacySettingsTargetsRepository
{
    public Task<int> UpsertTarget(int userId, int privacySettingId, string targetType, string targetValue)
    {
        // Используем специфичный для MySQL синтаксис UPSERT
        const string query = @"
            INSERT INTO PrivacySettingsTargets (UserId, PrivacySettingId, TargetType, TargetValue)
            VALUES (@userId, @privacySettingId, @targetType, @targetValue)
            ON DUPLICATE KEY UPDATE TargetValue = VALUES(TargetValue)";

        return dbConnection.ExecuteAsync(query, new { userId, privacySettingId, targetType, targetValue });
    }

    public Task<int> RemoveTarget(int privacySettingId, string targetValue)
    {
        const string query = "DELETE FROM PrivacySettingsTargets WHERE PrivacySettingId = @privacySettingId AND TargetValue = @targetValue";
        return dbConnection.ExecuteAsync(query, new { privacySettingId, targetValue });
    }

    public Task<bool> CheckTargetExists(int userId, string type)
    {
        const string query = "SELECT EXISTS(SELECT 1 FROM PrivacySettingsTargets WHERE UserId = @userId AND TargetType = @type LIMIT 1)";
        return dbConnection.ExecuteScalarAsync<bool>(query, new { userId, type });
    }

    public async Task<List<string>> GetAllUserTargets(int userId)
    {
        const string query = "SELECT TargetValue FROM PrivacySettingsTargets WHERE UserId = @userId";
        IEnumerable<string> result = await dbConnection.QueryAsync<string>(query, new { userId });
        return result.ToList();
    }
}
