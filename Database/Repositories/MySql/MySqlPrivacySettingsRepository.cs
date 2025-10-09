using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlPrivacySettingsRepository(IDbConnection dbConnection) : IPrivacySettingsRepository
{
    public Task<int> UpsertRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        // Используем специфичный для MySQL синтаксис UPSERT
        const string query = @"
            INSERT INTO PrivacySettings (UserId, Type, Action, IsActive, ActionCondition)
            VALUES (@userId, @type, @action, @isActive, @actionCondition)
            ON DUPLICATE KEY UPDATE
                Action = VALUES(Action),
                IsActive = VALUES(IsActive),
                ActionCondition = VALUES(ActionCondition)";
        
        return dbConnection.ExecuteAsync(query, new { userId, type, action, isActive, actionCondition });
    }

    public Task<int> DisableRule(int userId, string type)
    {
        const string query = "UPDATE PrivacySettings SET IsActive = 0 WHERE UserId = @userId AND Type = @type";
        return dbConnection.ExecuteAsync(query, new { userId, type });
    }
}