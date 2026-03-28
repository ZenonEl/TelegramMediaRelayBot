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

public class SqliteUserStateRepository(string connectionString) : IUserStateRepository
{
    private readonly string _connectionString = connectionString;

    public async Task SaveStateAsync(long chatId, string stateName, string stateDataJson, DateTime expiresAt)
    {
        const string query = @"
            INSERT INTO UserStates (ChatId, StateName, StateDataJson, ExpiresAt)
            VALUES (@chatId, @stateName, @stateDataJson, @expiresAt)
            ON CONFLICT(ChatId) DO UPDATE SET
                StateName = @stateName,
                StateDataJson = @stateDataJson,
                CreatedAt = datetime('now'),
                ExpiresAt = @expiresAt";

        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(query, new
        {
            chatId,
            stateName,
            stateDataJson,
            expiresAt = expiresAt.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    public async Task<(string StateName, string StateDataJson)?> GetStateAsync(long chatId)
    {
        const string query = @"
            SELECT StateName, StateDataJson
            FROM UserStates
            WHERE ChatId = @chatId
            AND ExpiresAt > datetime('now')";

        using var connection = new SqliteConnection(_connectionString);
        var result = await connection.QueryFirstOrDefaultAsync<StateRow>(query, new { chatId });

        if (result is null)
            return null;

        return (result.StateName, result.StateDataJson);
    }

    public async Task RemoveStateAsync(long chatId)
    {
        const string query = "DELETE FROM UserStates WHERE ChatId = @chatId";

        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(query, new { chatId });
    }

    public async Task<int> CleanupExpiredAsync()
    {
        const string query = "DELETE FROM UserStates WHERE ExpiresAt <= datetime('now')";

        using var connection = new SqliteConnection(_connectionString);
        return await connection.ExecuteAsync(query);
    }

    private class StateRow
    {
        public string StateName { get; set; } = string.Empty;
        public string StateDataJson { get; set; } = "{}";
    }
}
