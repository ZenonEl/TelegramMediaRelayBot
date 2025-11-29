// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqlitePrivacySettingsRepository(IDbConnection dbConnection) : IPrivacySettingsRepository
{
    public Task<int> UpsertRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        const string query = @"
            INSERT INTO PrivacySettings (UserId, Type, Action, IsActive, ActionCondition)
            VALUES (@userId, @type, @action, @isActive, @actionCondition)
            ON CONFLICT(UserId, Type) 
            DO UPDATE SET 
                Action = excluded.Action,
                IsActive = excluded.IsActive,
                ActionCondition = excluded.ActionCondition";
        
        return dbConnection.ExecuteAsync(query, new { userId, type, action, isActive, actionCondition });
    }

    public Task<int> DisableRule(int userId, string type)
    {
        const string query = "UPDATE PrivacySettings SET IsActive = 0 WHERE UserId = @userId AND Type = @type";
        return dbConnection.ExecuteAsync(query, new { userId, type });
    }
}