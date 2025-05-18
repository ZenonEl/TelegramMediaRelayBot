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

public class MySqlPrivacySettingsSetter : IPrivacySettingsSetter
{
    private readonly string _connectionString;

    public MySqlPrivacySettingsSetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        const string query = @$"
            INSERT INTO PrivacySettings (UserId, Type, Action, IsActive, ActionCondition)
            VALUES (@userId, @type, @action, @isActive, @actionCondition)
            ON DUPLICATE KEY UPDATE
            Action = @action,
            IsActive = @isActive,
            ActionCondition = @actionCondition";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteAsync(query, new {userId, type, action, isActive, actionCondition}) > 0;
    }

    public bool SetPrivacyRuleToDisabled(int userId, string type)
    {
        const string query = @"
            UPDATE PrivacySettings
            SET IsActive = 0
            WHERE UserId = @userId AND Type = @type";

        using var connection = new MySqlConnection(_connectionString);
        return connection.Execute(query, new {userId, type}) > 0;
    }
}

public class MySqlPrivacySettingsGetter : IPrivacySettingsGetter
{
    private readonly string _connectionString;

    public MySqlPrivacySettingsGetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool GetIsActivePrivacyRule(int userId, string type)
    {
        const string query = @"
            SELECT IsActive
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, type});
    }

    public async Task<int> GetPrivacyRuleId(int userId, string type)
    {
        const string query = @"
            SELECT ID
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(query, new {userId, type});
    }

    public async Task<string> GetPrivacyRuleValue(int userId, string type)
    {
        const string query = @"
            SELECT Action
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using var connection = new MySqlConnection(_connectionString);
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

        using var connection = new MySqlConnection(_connectionString);
        var result = await connection.QueryAsync<PrivacyRuleResult>(query, new {
            userId,
            actions = allowedActions
        });
        
        return result.ToList();
    }
}

