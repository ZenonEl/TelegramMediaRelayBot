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
using MySql.Data.MySqlClient;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlDefaultAction : IDefaultAction
{
    private readonly string _connectionString;

    public MySqlDefaultAction(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool AddDefaultUsersActionTargets(int userId, int actionId, string targetType, int targetId)
    {
        string query = @"
            INSERT INTO DefaultUsersActionTargets (UserId, ActionID, TargetType, TargetID) 
            VALUES (@userId, @actionId, @targetType, @targetId)";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, actionId, targetType, targetId});
    }

    public bool RemoveAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        string query = @"
            USE {Config.databaseName};
            DELETE FROM DefaultUsersActionTargets 
            WHERE UserId = @userId AND ActionID = @actionId AND TargetType = @targetType;";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, actionId, targetType});
    }
}

public class MySqlDefaultActionSetter : IDefaultActionSetter
{
    private readonly string _connectionString;

    public MySqlDefaultActionSetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool SetAutoSendVideoConditionToUser(int userId, string actionCondition, string type)
    {
        string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, ActionCondition) VALUES (@userId, @type, @actionCondition)
            ON DUPLICATE KEY UPDATE
                ActionCondition = @actionCondition";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, type, actionCondition});
    }

    public bool SetAutoSendVideoActionToUser(int userId, string action, string type)
    {
        string query = @"
            INSERT INTO DefaultUsersActions (UserId, Type, Action) VALUES (@userId, @type, @action)
            ON DUPLICATE KEY UPDATE
                Action = @action";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, type, action});
    }
}

public class MySqlDefaultActionGetter : IDefaultActionGetter
{
    private readonly string _connectionString;

    public MySqlDefaultActionGetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<int> GetAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        try 
        {
            using var connection = new MySqlConnection(_connectionString);
            return connection.Query<int>(
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
        string query = @$"
            SELECT ID FROM DefaultUsersActions 
            WHERE UserId = @userId AND Type = @type;";
        try
        {
            using var connection = new MySqlConnection(_connectionString);
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
            using var connection = new MySqlConnection(_connectionString);
            
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