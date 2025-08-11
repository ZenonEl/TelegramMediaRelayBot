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
using Microsoft.Data.Sqlite;
using TelegramMediaRelayBot.Database.Interfaces;


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
    private readonly TelegramMediaRelayBot.Database.UnitOfWork.IUnitOfWork? _uow;

    public SqliteDefaultActionSetter(string connectionString, TelegramMediaRelayBot.Database.UnitOfWork.IUnitOfWork? unitOfWork = null)
    {
        _connectionString = connectionString;
        _uow = unitOfWork;
    }

    public bool SetAutoSendVideoConditionToUser(int userId, string actionCondition, string type)
    {
        using var connection = _uow?.Connection as SqliteConnection ?? new SqliteConnection(_connectionString);
        _uow?.Begin();
        // First try to update the existing row for (UserId, Type)
        int affected = connection.Execute(
            "UPDATE DefaultUsersActions SET ActionCondition = @actionCondition WHERE UserId = @userId AND Type = @type AND IsActive = 1",
            new { userId, type, actionCondition }, _uow?.Transaction);
        // If no row was updated, insert a new one
        if (affected == 0)
        {
            affected = connection.Execute(
                "INSERT INTO DefaultUsersActions (UserId, Type, ActionCondition) VALUES (@userId, @type, @actionCondition)",
                new { userId, type, actionCondition }, _uow?.Transaction);
        }
        _uow?.Commit();
        return affected > 0;
    }

    public bool SetAutoSendVideoActionToUser(int userId, string action, string type)
    {
        using var connection = _uow?.Connection as SqliteConnection ?? new SqliteConnection(_connectionString);
        _uow?.Begin();
        // First try to update the existing row for (UserId, Type)
        int affected = connection.Execute(
            "UPDATE DefaultUsersActions SET Action = @action WHERE UserId = @userId AND Type = @type AND IsActive = 1",
            new { userId, type, action }, _uow?.Transaction);
        // If no row was updated, insert a new one
        if (affected == 0)
        {
            affected = connection.Execute(
                "INSERT INTO DefaultUsersActions (UserId, Type, Action) VALUES (@userId, @type, @action)",
                new { userId, type, action }, _uow?.Transaction);
        }
        _uow?.Commit();
        return affected > 0;
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
            WHERE UserId = @userId AND Type = @type
            ORDER BY ID DESC
            LIMIT 1";

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
            using var connection = new SqliteConnection(_connectionString);
            var result = await connection.QueryAsync<int>(
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
        const string query = @"SELECT ID FROM DefaultUsersActions WHERE UserId = @userId AND Type = @type ORDER BY ID DESC LIMIT 1";
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(query, new { userId, type });
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
            using var connection = new SqliteConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<(string Action, string ActionCondition)>(
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
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetDefaultActionByUserIDAndTypeAsync));
            return "";
        }
    }
}