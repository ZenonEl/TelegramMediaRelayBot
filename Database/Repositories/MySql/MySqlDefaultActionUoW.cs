using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlDefaultActionRepository(IDbConnection dbConnection) : IDefaultActionRepository
{
    public Task<int> UpsertActionCondition(int userId, string actionCondition, string type)
    {
        const string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, ActionCondition, IsActive)
            VALUES (@userId, @type, @actionCondition, 1)
            ON DUPLICATE KEY UPDATE ActionCondition = @actionCondition, IsActive = 1";
        
        return dbConnection.ExecuteAsync(query, new { userId, type, actionCondition });
    }

    public Task<int> UpsertAction(int userId, string action, string type)
    {
        const string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, Action, IsActive)
            VALUES (@userId, @type, @action, 1)
            ON DUPLICATE KEY UPDATE Action = @action, IsActive = 1";

        return dbConnection.ExecuteAsync(query, new { userId, type, action });
    }
}