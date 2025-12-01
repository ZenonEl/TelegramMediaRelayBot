// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql
{
    public class MySqlContactRepository(IDbConnection dbConnection) : IContactRepository
    {
        public Task<int> AddContactAsync(int userId, int contactId, string status)
        {
            const string query = "INSERT INTO Contacts (UserId, ContactId, Status) VALUES (@userId, @contactId, @status)";
            return dbConnection.ExecuteAsync(query, new { userId, contactId, status });
        }

        public async Task<int> UpsertMutedContactAsync(int mutedByUserId, int mutedContactId, DateTime? expirationDate)
        {
            const string cleanupSql = "DELETE FROM MutedContacts WHERE MutedByUserId = @mutedByUserId AND MutedContactId = @mutedContactId";
            await dbConnection.ExecuteAsync(cleanupSql, new { mutedByUserId, mutedContactId });
            const string insertSql = @"
                INSERT INTO MutedContacts (MutedByUserId, MutedContactId, MuteDate, ExpirationDate, IsActive)
                VALUES (@mutedByUserId, @mutedContactId, UTC_TIMESTAMP(), @expirationDate, 1)";

            return await dbConnection.ExecuteAsync(insertSql, new { mutedByUserId, mutedContactId, expirationDate });
        }

        public Task<int> DeactivateMutedContactAsync(int userId, int contactId)
        {
            const string query = "UPDATE MutedContacts SET IsActive = 0 WHERE MutedByUserId = @userId AND MutedContactId = @contactId";
            return dbConnection.ExecuteAsync(query, new { userId, contactId });
        }

        public Task<int> UnMuteUserByMuteId(int muteId)
        {
            const string query = @$"
                UPDATE MutedContacts SET IsActive = 0 WHERE MutedId = @muteId";

            return dbConnection.ExecuteAsync(query, new { muteId });
        }

        public Task<int> RemoveContactByStatusAsync(int senderId, int accepterId, string? status)
        {
            const string query = @"
                DELETE FROM Contacts
                WHERE (UserId = @senderId AND ContactId = @accepterId AND (@status IS NULL OR Status = @status))
                OR (UserId = @accepterId AND ContactId = @senderId AND (@status IS NULL OR Status = @status))";
            return dbConnection.ExecuteAsync(query, new { senderId, accepterId, status });
        }

        public Task<int> RemoveContactsBatchAsync(int userId, List<int> contactIds)
        {
            const string query = "DELETE FROM Contacts WHERE (UserId = @userId AND ContactId IN @contactIds) OR (ContactId = @userId AND UserId IN @contactIds)";
            return dbConnection.ExecuteAsync(query, new { userId, contactIds });
        }

        public Task<int> RemoveGroupMembersBatchAsync(int userId, List<int> contactIds)
        {
            const string query = "DELETE FROM GroupMembers WHERE (UserId = @userId AND ContactId IN @contactIds) OR (ContactId = @userId AND UserId IN @contactIds)";
            return dbConnection.ExecuteAsync(query, new { userId, contactIds });
        }

        public Task<int> RemoveAllContactsExceptAsync(int userId, List<int> excludeIds)
        {
            const string query = "DELETE FROM Contacts WHERE (UserId = @userId AND ContactId NOT IN @excludeIds) OR (ContactId = @userId AND UserId NOT IN @excludeIds)";
            return dbConnection.ExecuteAsync(query, new { userId, excludeIds });
        }

        public Task<int> RemoveAllGroupMembersExceptAsync(int userId, List<int> excludeIds)
        {
            const string query = "DELETE FROM GroupMembers WHERE (UserId = @userId AND ContactId NOT IN @excludeIds) OR (ContactId = @userId AND UserId NOT IN @excludeIds)";
            return dbConnection.ExecuteAsync(query, new { userId, excludeIds });
        }

        public Task<int> RemoveAllContactsAsync(int userId)
        {
            const string query = "DELETE FROM Contacts WHERE UserId = @userId OR ContactId = @userId";
            return dbConnection.ExecuteAsync(query, new { userId });
        }

        public Task<int> RemoveAllGroupMembersAsync(int userId)
        {
            const string query = "DELETE FROM GroupMembers WHERE UserId = @userId OR ContactId = @userId";
            return dbConnection.ExecuteAsync(query, new { userId });
        }

        public Task<int> UpdateContactStatusAsync(int userId, int contactId, string status)
        {
            const string query = "UPDATE Contacts SET Status = @status WHERE UserId = @userId AND ContactId = @contactId";
            return dbConnection.ExecuteAsync(query, new { userId, contactId, status });
        }
    }
}
