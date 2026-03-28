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
using TelegramMediaRelayBot.Domain.Models;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteUserSettingsRepository(string connectionString) : IUserSettingsRepository
{
    private readonly string _connectionString = connectionString;

    public async Task<UserSettings> GetSettingsAsync(int userId)
    {
        const string query = "SELECT SettingsJson FROM Users WHERE ID = @userId";

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            var json = await connection.ExecuteScalarAsync<string>(query, new { userId });
            return UserSettings.FromJson(json ?? "{}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get settings for user {UserId}", userId);
            return new UserSettings();
        }
    }

    public async Task<bool> SaveSettingsAsync(int userId, UserSettings settings)
    {
        const string query = "UPDATE Users SET SettingsJson = @json WHERE ID = @userId";

        try
        {
            var json = settings.ToJson();
            using var connection = new SqliteConnection(_connectionString);
            var affected = await connection.ExecuteAsync(query, new { json, userId });
            return affected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings for user {UserId}", userId);
            return false;
        }
    }
}
