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

    public bool SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        string query = @$"
            INSERT INTO PrivacySettings (UserId, Type, Action, IsActive, ActionCondition)
            VALUES (@userId, @type, @action, @isActive, @actionCondition)
            ON DUPLICATE KEY UPDATE
            Action = @action,
            IsActive = @isActive,
            ActionCondition = @actionCondition";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, type, action, isActive, actionCondition});
    }

    public bool SetPrivacyRuleToDisabled(int userId, string type)
    {
        string query = @"
            UPDATE PrivacySettings
            SET IsActive = 0
            WHERE UserId = @userId AND Type = @type";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, type});
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
        string query = @"
            SELECT IsActive
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new {userId, type});
    }
}

