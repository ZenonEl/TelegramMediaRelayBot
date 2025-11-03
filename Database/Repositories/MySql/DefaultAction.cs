// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Dapper;
using System.Data;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlDefaultAction(IDefaultActionTargetsRepository repository) : IDefaultAction
{
    public async Task<bool> AddDefaultUsersActionTargets(int userId, int actionId, string targetType, int targetId)
    {
        var affectedRows = await repository.AddTarget(userId, actionId, targetType, targetId);
        return affectedRows > 0;
    }

    public async Task<bool> RemoveAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        var affectedRows = await repository.RemoveAllTargets(userId, targetType, actionId);
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
                new { 
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
    
            return dbConnection.ExecuteScalar<int>(query, new {userId, type});
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
    

            var result = dbConnection.QueryFirstOrDefault<(string Action, string ActionCondition)>(
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
    
            var result = await dbConnection.QueryAsync<int>(
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
    
            var result = await dbConnection.QueryFirstOrDefaultAsync<(string Action, string ActionCondition)>(
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