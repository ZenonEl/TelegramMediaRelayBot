// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Dapper;
using System.Data;
using Microsoft.Data.Sqlite;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteContactGroupRepository(IDbConnection dbConnection) : IContactGroupRepository
{
    public bool AddContactToGroup(int userId, int contactId, int groupId)
    {
        const string query = @"
            INSERT OR IGNORE INTO GroupMembers (UserId, ContactId, GroupId)
            VALUES (@userId, @contactId, @groupId);";

        return dbConnection.Execute(query, new { userId, contactId, groupId }) > 0;
    }

    public bool RemoveContactFromGroup(int userId, int contactId, int groupId)
    {
        const string query = @"
            DELETE FROM GroupMembers
            WHERE UserId = @userId AND ContactId = @contactId AND GroupId = @groupId";

        return dbConnection.Execute(query, new { userId, contactId, groupId }) > 0;
    }

    public bool CheckUserAndContactConnect(int userId, int contactId)
    {
        const string query = @"
            SELECT COUNT(*) FROM Contacts
            WHERE 
                ((UserId = @userId AND ContactId = @contactId) OR (UserId = @contactId AND ContactId = @userId))
                AND Status = @status";

        int count = dbConnection.ExecuteScalar<int>(query, new
        {
            userId,
            contactId,
            status = ContactsStatus.ACCEPTED
        });

        return count > 0;
    }
}