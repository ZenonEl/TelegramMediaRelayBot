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

public class SqlitePrivacySettingsSetter : IPrivacySettingsSetter
{
    private readonly string _connectionString;
    private readonly TelegramMediaRelayBot.Database.UnitOfWork.IUnitOfWork? _uow;

    public SqlitePrivacySettingsSetter(string connectionString, TelegramMediaRelayBot.Database.UnitOfWork.IUnitOfWork? unitOfWork = null)
    {
        _connectionString = connectionString;
        _uow = unitOfWork;
    }

    public async Task<bool> SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        using var connection = _uow?.Connection as SqliteConnection ?? new SqliteConnection(_connectionString);
        _uow?.Begin();
        // Try update first to avoid duplicates even when unique index is missing
        var updated = await connection.ExecuteAsync(
            @"UPDATE PrivacySettings
              SET Action = @action,
                  IsActive = @isActive,
                  ActionCondition = @actionCondition
              WHERE UserId = @userId AND Type = @type",
            new { userId, type, action, isActive, actionCondition }, _uow?.Transaction);

        if (updated == 0)
        {
            var inserted = await connection.ExecuteAsync(
                @"INSERT INTO PrivacySettings (UserId, Type, Action, IsActive, ActionCondition)
                  VALUES (@userId, @type, @action, @isActive, @actionCondition)",
                new { userId, type, action, isActive, actionCondition }, _uow?.Transaction);
            _uow?.Commit();
            return inserted > 0;
        }

        _uow?.Commit();
        return updated > 0;
    }

    public bool SetPrivacyRuleToDisabled(int userId, string type)
    {
        const string query = @"
            UPDATE PrivacySettings
            SET IsActive = 0
            WHERE UserId = @userId AND Type = @type";

        using var connection = _uow?.Connection as SqliteConnection ?? new SqliteConnection(_connectionString);
        try
        {
            _uow?.Begin();
            var ok = connection.Execute(query, new {userId, type}, _uow?.Transaction) > 0;
            _uow?.Commit();
            return ok;
        }
        catch
        {
            _uow?.Rollback();
            throw;
        }
    }
}

public class SqlitePrivacySettingsGetter : IPrivacySettingsGetter
{
    private readonly string _connectionString;

    public SqlitePrivacySettingsGetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool GetIsActivePrivacyRule(int userId, string type)
    {
        const string query = @"
            SELECT IsActive
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, type});
    }

    public async Task<int> GetPrivacyRuleId(int userId, string type)
    {
        const string query = @"
            SELECT ID
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using var connection = new SqliteConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(query, new {userId, type});
    }

    public async Task<string> GetPrivacyRuleValue(int userId, string type)
    {
        const string query = @"
            SELECT Action
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using var connection = new SqliteConnection(_connectionString);
        return await connection.ExecuteScalarAsync<string>(query, new {userId, type}) ?? string.Empty;
    }

    public async Task<List<PrivacyRuleResult>> GetAllActiveUserRulesWithTargets(int userId)
    {
        const string query = @"
            SELECT 
                PS.Type,
                PS.Action,
                PST.TargetValue
            FROM PrivacySettings PS
            LEFT JOIN PrivacySettingsTargets PST ON PS.Id = PST.PrivacySettingId
            WHERE PS.UserId = @userId 
                AND PS.IsActive = 1
                AND PS.Action IN @actions";

        var allowedActions = new[] {
            PrivacyRuleAction.SOCIAL_FILTER,
            PrivacyRuleAction.NSFW_FILTER,
            PrivacyRuleAction.UNIFIED_FILTER,
            PrivacyRuleAction.DOMAIN_FILTER
        };

        using var connection = new SqliteConnection(_connectionString);
        var result = await connection.QueryAsync<PrivacyRuleResult>(query, new {
            userId,
            actions = allowedActions
        });
        
        return result.ToList();
    }
}