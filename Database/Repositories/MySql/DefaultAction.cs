// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlDefaultAction(IDefaultActionTargetsRepository repository) : IDefaultAction
{
    public async Task<bool> AddDefaultUsersActionTargets(int userId, int actionId, string targetType, int targetId)
    {
        int affectedRows = await repository.AddTarget(userId, actionId, targetType, targetId);
        return affectedRows > 0;
    }

    public async Task<bool> RemoveAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        int affectedRows = await repository.RemoveAllTargets(userId, targetType, actionId);
        return affectedRows > 0;
    }
}

public class MySqlDefaultActionSetter(IDefaultActionUoW uowService) : IDefaultActionSetter
{
    // Метод-прослойка, делегирующий вызов
    public Task<bool> SetAutoSendVideoConditionToUser(int userId, string actionCondition, string type)
    {
        return uowService.SetAutoSendVideoCondition(userId, actionCondition, type);
    }

    // Метод-прослойка, делегирующий вызов
    public Task<bool> SetAutoSendVideoActionToUser(int userId, string action, string type)
    {
        return uowService.SetAutoSendVideoAction(userId, action, type);
    }
}

public class MySqlDefaultActionGetter(IDbConnection dbConnection) : IDefaultActionGetter
{
    public List<int> GetAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        try
        {
            return dbConnection.Query<int>(
                @"SELECT TargetID FROM DefaultUsersActionTargets
                WHERE UserId = @userId AND TargetType = @targetType AND ActionID = @actionId",
                new
                {
                    userId,
                    targetType,
                    actionId
                }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error on getting data: {ex.Message}");
            return new List<int>();
        }
    }

    public int GetDefaultActionId(int userId, string type)
    {
        const string query = @$"
            SELECT ID FROM DefaultUsersActions
            WHERE UserId = @userId AND Type = @type
            ORDER BY ID DESC
            LIMIT 1;";
        try
        {

            return dbConnection.ExecuteScalar<int>(query, new { userId, type });
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


            (string Action, string ActionCondition) result = dbConnection.QueryFirstOrDefault<(string Action, string ActionCondition)>(
                @"SELECT Action, ActionCondition
                FROM DefaultUsersActions
                WHERE UserID = @userID
                AND Type = @type
                AND IsActive = 1
                ORDER BY ID DESC
                LIMIT 1",
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

    public async Task<List<int>> GetAllDefaultUsersActionTargetsAsync(int userId, string targetType, int actionId)
    {
        try
        {

            IEnumerable<int> result = await dbConnection.QueryAsync<int>(
                @"SELECT TargetID FROM DefaultUsersActionTargets
                WHERE UserId = @userId AND TargetType = @targetType AND ActionID = @actionId",
                new { userId, targetType, actionId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error on getting data: {ex.Message}");
            return new List<int>();
        }
    }

    public async Task<int> GetDefaultActionIdAsync(int userId, string type)
    {
        const string query = @"SELECT ID FROM DefaultUsersActions WHERE UserId = @userId AND Type = @type ORDER BY ID DESC LIMIT 1;";
        try
        {

            return await dbConnection.ExecuteScalarAsync<int>(query, new { userId, type });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on getting data: {ex.Message}");
            return 0;
        }
    }

    public async Task<string> GetDefaultActionByUserIDAndTypeAsync(int userID, string type)
    {
        try
        {

            (string Action, string ActionCondition) result = await dbConnection.QueryFirstOrDefaultAsync<(string Action, string ActionCondition)>(
                @"SELECT Action, ActionCondition
                FROM DefaultUsersActions
                WHERE UserID = @userID
                AND Type = @type
                AND IsActive = 1
                ORDER BY ID DESC
                LIMIT 1",
                new { userID, type });
            return result != default ? $"{result.Action};{result.ActionCondition}" : UsersAction.NO_VALUE;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetDefaultActionByUserIDAndTypeAsync));
            return "";
        }
    }
}
