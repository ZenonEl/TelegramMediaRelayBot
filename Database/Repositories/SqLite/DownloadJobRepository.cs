// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteDownloadJobRepository : IDownloadJobRepository
{
    private readonly string _connectionString;

    public SqliteDownloadJobRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Add(DownloadJob job)
    {
        const string query = @"
            INSERT INTO DownloadJobs (Id, ChatId, Url, Caption, TargetUserIdsJson, IsGroupChat)
            VALUES (@Id, @ChatId, @Url, @Caption, @TargetUserIdsJson, @IsGroupChat)";

        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(query, new
        {
            job.Id,
            job.ChatId,
            job.Url,
            job.Caption,
            TargetUserIdsJson = job.TargetUserIds is null ? null : JsonSerializer.Serialize(job.TargetUserIds),
            IsGroupChat = job.IsGroupChat ? 1 : 0
        });
    }

    public void Remove(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("DELETE FROM DownloadJobs WHERE Id = @id", new { id });
    }

    public List<DownloadJob> GetAll()
    {
        const string query = @"
            SELECT Id, ChatId, Url, Caption, TargetUserIdsJson, IsGroupChat
            FROM DownloadJobs ORDER BY CreatedAt";

        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<(string Id, long ChatId, string Url, string Caption, string? TargetUserIdsJson, long IsGroupChat)>(query)
            .Select(row => new DownloadJob(
                row.Id,
                row.ChatId,
                row.Url,
                row.Caption,
                row.TargetUserIdsJson is null ? null : JsonSerializer.Deserialize<List<long>>(row.TargetUserIdsJson),
                row.IsGroupChat != 0))
            .ToList();
    }
}
