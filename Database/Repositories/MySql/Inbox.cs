// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using Dapper;
using MySql.Data.MySqlClient;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlInboxRepository : IInboxRepository
{
    private readonly string _connectionString;
    public MySqlInboxRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<long> AddItemAsync(int ownerUserId, int fromContactId, string caption, string payloadJson, string status)
    {
        await using var conn = new MySqlConnection(_connectionString);
        var sql = @"INSERT INTO InboxItems (OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt)
                    VALUES (@ownerUserId, @fromContactId, @caption, @payloadJson, @status, UTC_TIMESTAMP());
                    SELECT LAST_INSERT_ID();";
        var id = await conn.ExecuteScalarAsync<long>(sql, new { ownerUserId, fromContactId, caption, payloadJson, status });
        return id;
    }

    public async Task<bool> SetStatusAsync(long inboxItemId, string status)
    {
        await using var conn = new MySqlConnection(_connectionString);
        var sql = "UPDATE InboxItems SET Status=@status WHERE ID=@id";
        var affected = await conn.ExecuteAsync(sql, new { id = inboxItemId, status });
        return affected > 0;
    }

    public async Task<IEnumerable<InboxItemDto>> GetItemsAsync(int ownerUserId, int limit = 20, int offset = 0)
    {
        await using var conn = new MySqlConnection(_connectionString);
        var sql = @"SELECT ID as Id, OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt
                    FROM InboxItems WHERE OwnerUserId=@ownerUserId
                    ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset";
        return await conn.QueryAsync<InboxItemDto>(sql, new { ownerUserId, limit, offset });
    }

    public async Task<InboxItemDto?> GetItemAsync(long id)
    {
        await using var conn = new MySqlConnection(_connectionString);
        var sql = @"SELECT ID as Id, OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt
                    FROM InboxItems WHERE ID=@id";
        return (await conn.QueryAsync<InboxItemDto>(sql, new { id })).FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var conn = new MySqlConnection(_connectionString);
        var affected = await conn.ExecuteAsync("DELETE FROM InboxItems WHERE ID=@id", new { id });
        return affected > 0;
    }

    public async Task<int> GetNewCountAsync(int ownerUserId)
    {
        await using var conn = new MySqlConnection(_connectionString);
        var sql = "SELECT COUNT(*) FROM InboxItems WHERE OwnerUserId=@ownerUserId AND Status='new'";
        return await conn.ExecuteScalarAsync<int>(sql, new { ownerUserId });
    }
}

