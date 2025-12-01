// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlContactGroupRepository(IDbConnection dbConnection) : IContactGroupRepository
{


    public bool AddContactToGroup(int userId, int contactId, int groupId)
    {
        string query = @"
            INSERT IGNORE INTO GroupMembers (UserId, ContactId, GroupId)
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


        dbConnection.Open();

        int count = dbConnection.ExecuteScalar<int>(query, new
        {
            userId,
            contactId,
            status = ContactsStatus.ACCEPTED
        });

        return count > 0;
    }
}
