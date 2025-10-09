using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteDefaultActionRepository(IDbConnection dbConnection) : IDefaultActionRepository
{
    public Task<int> UpsertActionCondition(int userId, string actionCondition, string type)
    {
        const string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, ActionCondition, IsActive)
            VALUES (@userId, @type, @actionCondition, 1)
            ON CONFLICT(UserId, Type) 
            DO UPDATE SET ActionCondition = excluded.ActionCondition, IsActive = 1";
        
        return dbConnection.ExecuteAsync(query, new { userId, type, actionCondition });
    }

    public Task<int> UpsertAction(int userId, string action, string type)
    {
        const string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, Action, IsActive)
            VALUES (@userId, @type, @action, 1)
            ON CONFLICT(UserId, Type) 
            DO UPDATE SET Action = excluded.Action, IsActive = 1";

        return dbConnection.ExecuteAsync(query, new { userId, type, action });
    }
}