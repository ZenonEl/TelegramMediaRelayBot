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

public class MySqlPrivacySettingsTargetsSetter : IPrivacySettingsTargetsSetter
{
    private readonly string _connectionString;

    public MySqlPrivacySettingsTargetsSetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> SetPrivacyRuleTarget(int userId, int privacySettingId, string targetType, string targetValue)
    {
        const string query = @$"
            INSERT INTO PrivacySettingsTargets (UserId, PrivacySettingId, TargetType, TargetValue)
            VALUES (@userId, @privacySettingId, @targetType, @targetValue)
            ON DUPLICATE KEY UPDATE
            TargetValue = @targetValue";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteAsync(query, new {userId, privacySettingId, targetType, targetValue}) > 0;
    }

    public async Task<bool> SetToRemovePrivacyRuleTarget(int privacySettingId, string targetValue)
    {
        const string query = @$"
            DELETE FROM PrivacySettingsTargets
            WHERE PrivacySettingId = @privacySettingId AND TargetValue = @targetValue";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteAsync(query, new { privacySettingId, targetValue }) > 0;
    }
}

public class MySqlPrivacySettingsTargetsGetter : IPrivacySettingsTargetsGetter
{
    private readonly string _connectionString;

    public MySqlPrivacySettingsTargetsGetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> CheckPrivacyRuleTargetExists(int userId, string type)
    {
        const string query = @"
            SELECT EXISTS(
                SELECT 1 FROM PrivacySettingsTargets
                WHERE UserId = @userId AND TargetType = @type
                LIMIT 1
            )";

        using var connection = new MySqlConnection(_connectionString);
        var result = await connection.ExecuteScalarAsync<bool>(query, new { userId, type });
        return result;
    }

    public async Task<List<string>> GetAllActiveUserRuleTargets(int userId)
    {
        const string query = @"
            SELECT TargetValue
            FROM PrivacySettingsTargets
            WHERE UserId = @userId";

        using var connection = new MySqlConnection(_connectionString);
        return (List<string>)await connection.QueryAsync<List<string>>(query, new { userId });
    }
}
