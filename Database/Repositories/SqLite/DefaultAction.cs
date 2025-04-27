using Dapper;
using Microsoft.Data.Sqlite;
using TelegramMediaRelayBot.Database.Interfaces;
using System.Data;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteDefaultAction : IDefaultAction
{
    private readonly string _connectionString;

    public SqliteDefaultAction(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool AddDefaultUsersActionTargets(int userId, int actionId, string targetType, int targetId)
    {
        const string query = @"
            INSERT INTO DefaultUsersActionTargets (UserId, ActionID, TargetType, TargetID) 
            VALUES (@userId, @actionId, @targetType, @targetId)";

        using var connection = new SqliteConnection(_connectionString);
        return connection.Execute(query, new {userId, actionId, targetType, targetId}) > 0;
    }

    public bool RemoveAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        const string query = @"
            DELETE FROM DefaultUsersActionTargets 
            WHERE UserId = @userId AND ActionID = @actionId AND TargetType = @targetType";

        using var connection = new SqliteConnection(_connectionString);
        return connection.Execute(query, new {userId, actionId, targetType}) > 0;
    }
}

public class SqliteDefaultActionSetter : IDefaultActionSetter
{
    private readonly string _connectionString;

    public SqliteDefaultActionSetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool SetAutoSendVideoConditionToUser(int userId, string actionCondition, string type)
    {
        const string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, ActionCondition) 
            VALUES (@userId, @type, @actionCondition)
            ON CONFLICT(UserId, Type) 
            DO UPDATE SET ActionCondition = @actionCondition";

        using var connection = new SqliteConnection(_connectionString);
        return connection.Execute(query, new {userId, type, actionCondition}) > 0;
    }

    public bool SetAutoSendVideoActionToUser(int userId, string action, string type)
    {
        const string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, Action) 
            VALUES (@userId, @type, @action)
            ON CONFLICT(UserId, Type) 
            DO UPDATE SET Action = @action";

        using var connection = new SqliteConnection(_connectionString);
        return connection.Execute(query, new {userId, type, action}) > 0;
    }
}

public class SqliteDefaultActionGetter : IDefaultActionGetter
{
    private readonly string _connectionString;

    public SqliteDefaultActionGetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<int> GetAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        try 
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<int>(
                @"SELECT TargetID FROM DefaultUsersActionTargets 
                WHERE UserId = @userId AND TargetType = @targetType AND ActionID = @actionId",
                new { userId, targetType, actionId }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error on getting data: {ex.Message}");
            return new List<int>();
        }
    }

    public int GetDefaultActionId(int userId, string type)
    {
        const string query = @"
            SELECT ID FROM DefaultUsersActions 
            WHERE UserId = @userId AND Type = @type";

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.ExecuteScalar<int>(query, new {userId, type});
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on getting data: {ex.Message}");
            return 0;
        }
    }

    public string GetDefaultActionByUserIDAndType(int userID, string type)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            
            var result = connection.QueryFirstOrDefault<(string Action, string ActionCondition)>(
                @"SELECT Action, ActionCondition
                FROM DefaultUsersActions
                WHERE UserID = @userID
                AND Type = @type
                AND IsActive = 1",
                new { userID, type });

            return result != default 
                ? $"{result.Action};{result.ActionCondition}" 
                : UsersAction.NO_VALUE;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetDefaultActionByUserIDAndType));
            return "";
        }
    }
}