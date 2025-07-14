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

public class MySqlContactGroupRepository(string connectionString) : IContactGroupRepository
{
    private readonly string _connectionString = connectionString;

    public bool AddContactToGroup(int userId, int contactId, int groupId)
    {
        string query = @"
            INSERT IGNORE INTO GroupMembers (UserId, ContactId, GroupId)
            VALUES (@userId, @contactId, @groupId);";

        using var connection = new MySqlConnection(_connectionString);
        return connection.Execute(query, new { userId, contactId, groupId }) > 0;
    }

    public bool RemoveContactFromGroup(int userId, int contactId, int groupId)
    {
        const string query = @"
            DELETE FROM GroupMembers
            WHERE UserId = @userId AND ContactId = @contactId AND GroupId = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        return connection.Execute(query, new { userId, contactId, groupId }) > 0;
    }

    public bool CheckUserAndContactConnect(int userId, int contactId)
    {
        const string query = @"
            SELECT COUNT(*) FROM Contacts
            WHERE 
                ((UserId = @userId AND ContactId = @contactId) OR (UserId = @contactId AND ContactId = @userId))
                AND Status = @status";

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        int count = connection.ExecuteScalar<int>(query, new
        {
            userId,
            contactId,
            status = ContactsStatus.ACCEPTED
        });

        return count > 0;
    }
}