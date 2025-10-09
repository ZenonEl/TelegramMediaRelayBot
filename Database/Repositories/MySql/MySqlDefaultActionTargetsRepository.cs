using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlDefaultActionTargetsRepository(IDbConnection dbConnection) : IDefaultActionTargetsRepository
{
    public Task<int> AddTarget(int userId, int actionId, string targetType, int targetId)
    {
        const string query = @"
            INSERT INTO DefaultUsersActionTargets (UserId, ActionID, TargetType, TargetID) 
            VALUES (@userId, @actionId, @targetType, @targetId)";
        return dbConnection.ExecuteAsync(query, new { userId, actionId, targetType, targetId });
    }

    public Task<int> RemoveAllTargets(int userId, string targetType, int actionId)
    {
        const string query = @"
            DELETE FROM DefaultUsersActionTargets 
            WHERE UserId = @userId AND ActionID = @actionId AND TargetType = @targetType";
        return dbConnection.ExecuteAsync(query, new { userId, actionId, targetType });
    }
}