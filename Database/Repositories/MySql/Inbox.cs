// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlInboxRepository(IDbConnection dbConnection) : IInboxRepository
{
    public async Task<long> AddItemAsync(int ownerUserId, int fromContactId, string caption, string payloadJson, string status)
    {

        var sql = @"INSERT INTO InboxItems (OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt)
                    VALUES (@ownerUserId, @fromContactId, @caption, @payloadJson, @status, UTC_TIMESTAMP());
                    SELECT LAST_INSERT_ID();";
        var id = await dbConnection.ExecuteScalarAsync<long>(sql, new { ownerUserId, fromContactId, caption, payloadJson, status });
        return id;
    }

    public async Task<bool> SetStatusAsync(long inboxItemId, string status)
    {

        var sql = "UPDATE InboxItems SET Status=@status WHERE ID=@id";
        var affected = await dbConnection.ExecuteAsync(sql, new { id = inboxItemId, status });
        return affected > 0;
    }

    public async Task<IEnumerable<InboxItemDto>> GetItemsAsync(int ownerUserId, int limit = 20, int offset = 0)
    {

        var sql = @"SELECT ID as Id, OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt
                    FROM InboxItems WHERE OwnerUserId=@ownerUserId
                    ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset";
        return await dbConnection.QueryAsync<InboxItemDto>(sql, new { ownerUserId, limit, offset });
    }

    public async Task<IEnumerable<InboxItemDto>> GetItemsAsync(int ownerUserId, string? statusFilter, int? fromContactId, int limit = 20, int offset = 0)
    {

        var conditions = new List<string> { "OwnerUserId=@ownerUserId" };
        if (!string.IsNullOrWhiteSpace(statusFilter)) conditions.Add("Status=@status");
        if (fromContactId.HasValue) conditions.Add("FromContactId=@fromContactId");
        var where = string.Join(" AND ", conditions);
        var sql = $@"SELECT ID as Id, OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt
                    FROM InboxItems WHERE {where}
                    ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset";
        return await dbConnection.QueryAsync<InboxItemDto>(sql, new { ownerUserId, status = statusFilter, fromContactId, limit, offset });
    }

    public async Task<InboxItemDto?> GetItemAsync(long id)
    {

        var sql = @"SELECT ID as Id, OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt
                    FROM InboxItems WHERE ID=@id";
        return (await dbConnection.QueryAsync<InboxItemDto>(sql, new { id })).FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(long id)
    {

        var affected = await dbConnection.ExecuteAsync("DELETE FROM InboxItems WHERE ID=@id", new { id });
        return affected > 0;
    }

    public async Task<int> GetNewCountAsync(int ownerUserId)
    {

        var sql = "SELECT COUNT(*) FROM InboxItems WHERE OwnerUserId=@ownerUserId AND Status='new'";
        return await dbConnection.ExecuteScalarAsync<int>(sql, new { ownerUserId });
    }

    public async Task<int> SetStatusForOwnerAsync(int ownerUserId, string fromStatus, string toStatus, int? fromContactId = null)
    {

        var sql = "UPDATE InboxItems SET Status=@toStatus WHERE OwnerUserId=@ownerUserId AND Status=@fromStatus" + (fromContactId.HasValue ? " AND FromContactId=@fromContactId" : "");
        return await dbConnection.ExecuteAsync(sql, new { ownerUserId, fromStatus, toStatus, fromContactId });
    }

    public async Task<int> DeleteForOwnerAsync(int ownerUserId, string? statusFilter = null, int? fromContactId = null)
    {

        var conditions = new List<string> { "OwnerUserId=@ownerUserId" };
        if (!string.IsNullOrWhiteSpace(statusFilter)) conditions.Add("Status=@status");
        if (fromContactId.HasValue) conditions.Add("FromContactId=@fromContactId");
        var where = string.Join(" AND ", conditions);
        var sql = $"DELETE FROM InboxItems WHERE {where}";
        return await dbConnection.ExecuteAsync(sql, new { ownerUserId, status = statusFilter, fromContactId });
    }

    public async Task<IEnumerable<InboxSenderInfo>> GetSendersAsync(int ownerUserId)
    {

        var sql = @"SELECT FromContactId,
                           COUNT(*) as Total,
                           SUM(CASE WHEN Status='new' THEN 1 ELSE 0 END) as NewCount
                    FROM InboxItems
                    WHERE OwnerUserId=@ownerUserId
                    GROUP BY FromContactId
                    ORDER BY MAX(CreatedAt) DESC";
        return await dbConnection.QueryAsync<InboxSenderInfo>(sql, new { ownerUserId });
    }

        public async Task<InboxItemDto?> GetLatestItemForOwnerFromAsync(int ownerUserId, int fromContactId)
        {

            var sql = @"SELECT ID as Id, OwnerUserId, FromContactId, Caption, PayloadJson, Status, CreatedAt
                        FROM InboxItems WHERE OwnerUserId=@ownerUserId AND FromContactId=@fromContactId
                        ORDER BY CreatedAt DESC LIMIT 1";
            return (await dbConnection.QueryAsync<InboxItemDto>(sql, new { ownerUserId, fromContactId })).FirstOrDefault();
        }

        public async Task<bool> UpdatePayloadAsync(long inboxItemId, string payloadJson)
        {

            var sql = "UPDATE InboxItems SET PayloadJson=@payloadJson WHERE ID=@id";
            var affected = await dbConnection.ExecuteAsync(sql, new { id = inboxItemId, payloadJson });
            return affected > 0;
        }
}

