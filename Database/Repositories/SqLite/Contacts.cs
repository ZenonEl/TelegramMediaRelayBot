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

public class SqliteContactAdder(string connectionString) : IContactAdder
{
    private readonly string _connectionString = connectionString;

    public void AddContact(long telegramID, string link)
    {
        const string query = @"
            INSERT INTO Contacts (UserId, ContactId, Status) 
            VALUES (@userId, @contactId, @status)";
        SqliteContactGetter contactGetter = new(_connectionString);

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Execute(query, new 
                {
                    userId = contactGetter.GetContactByTelegramID(telegramID),
                    contactId = contactGetter.GetContactIDByLink(link),
                    status = ContactsStatus.WAITING_FOR_ACCEPT
                });
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
            }
        }
    }

    public bool AddMutedContact(int mutedByUserId, int mutedContactId, DateTime? expirationDate = null, DateTime muteDate = default)
    {
        if (muteDate == default)
        {
            muteDate = DateTime.Now;
        }

        const string query = @"
            INSERT INTO MutedContacts (MutedByUserId, MutedContactId, MuteDate, ExpirationDate)
            VALUES (@mutedByUserId, @mutedContactId, @muteDate, @expirationDate)
            ON CONFLICT(MutedByUserId, MutedContactId) 
            DO UPDATE SET
                MuteDate = @muteDate,
                ExpirationDate = @expirationDate,
                IsActive = 1";

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Execute(query, new 
                {
                    mutedByUserId,
                    mutedContactId,
                    muteDate,
                    expirationDate
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
                return false;
            }
        }
    }
}

public class SqliteContactRemover(string connectionString) : IContactRemover
{
    private readonly string _connectionString = connectionString;

    public void RemoveMutedContact(int userId, int contactId)
    {
        const string query = @"
            UPDATE MutedContacts
            SET IsActive = 0
            WHERE MutedByUserId = @userId AND MutedContactId = @contactId";

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Execute(query, new { userId, contactId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(RemoveMutedContact));
            }
        }
    }

    public bool RemoveContactByStatus(int senderTelegramID, int accepterTelegramID, string? status = null)
    {
        const string query = @"
            DELETE FROM Contacts
            WHERE (UserId = @senderTelegramID AND ContactId = @accepterTelegramID AND (@status IS NULL OR Status = @status))
            OR (UserId = @accepterTelegramID AND ContactId = @senderTelegramID AND (@status IS NULL OR Status = @status))";

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Execute(query, new 
                {
                    senderTelegramID,
                    accepterTelegramID,
                    status
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
                return false;
            }
        }
    }

    public bool RemoveUsersFromContacts(int userId, List<int> contactIds)
    {
        if (contactIds == null || contactIds.Count == 0)
        {
            return true;
        }

        string contactIdParams = string.Join(", ", contactIds.Select((_, i) => $"@contactId{i}"));

        string deleteContactsQuery = @$"
            DELETE FROM Contacts
            WHERE (UserId = @userId AND ContactId IN ({contactIdParams}))
            OR (ContactId = @userId AND UserId IN ({contactIdParams}));";

        string deleteGroupMembersQuery = @$"
            DELETE FROM GroupMembers
            WHERE (UserId = @userId AND ContactId IN ({contactIdParams}))
            OR (ContactId = @userId AND UserId IN ({contactIdParams}));";

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("userId", userId);
                        for (int i = 0; i < contactIds.Count; i++)
                        {
                            parameters.Add($"contactId{i}", contactIds[i]);
                        }

                        connection.Execute(deleteContactsQuery, parameters, transaction);
                        connection.Execute(deleteGroupMembersQuery, parameters, transaction);
                        
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error("Error deleting from database: " + ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error connecting to database: " + ex.Message);
                return false;
            }
        }
    }

    public bool RemoveAllContactsExcept(int userId, List<int> excludeIds)
    {
        if (excludeIds == null || excludeIds.Count == 0)
        {
            return RemoveAllContacts(userId);
        }

        string excludeParams = string.Join(",", excludeIds.Select((_, i) => $"@excludeId{i}"));

        string deleteContactsQuery = @$"
            DELETE FROM Contacts
            WHERE (UserId = @userId AND ContactId NOT IN ({excludeParams}))
            OR (ContactId = @userId AND UserId NOT IN ({excludeParams}));";

        string deleteGroupMembersQuery = @$"
            DELETE FROM GroupMembers
            WHERE (UserId = @userId AND ContactId NOT IN ({excludeParams}))
            OR (ContactId = @userId AND UserId NOT IN ({excludeParams}));";

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("userId", userId);
                        for (int i = 0; i < excludeIds.Count; i++)
                        {
                            parameters.Add($"excludeId{i}", excludeIds[i]);
                        }

                        connection.Execute(deleteContactsQuery, parameters, transaction);
                        connection.Execute(deleteGroupMembersQuery, parameters, transaction);
                        
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error("Error deleting from database: " + ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error connecting to database: " + ex.Message);
                return false;
            }
        }
    }

    public bool RemoveAllContacts(int userId)
    {
        const string deleteContactsQuery = @"
            DELETE FROM Contacts
            WHERE UserId = @userId OR ContactId = @userId;";

        const string deleteGroupMembersQuery = @"
            DELETE FROM GroupMembers
            WHERE UserId = @userId OR ContactId = @userId;";

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        connection.Execute(deleteContactsQuery, new { userId }, transaction);
                        connection.Execute(deleteGroupMembersQuery, new { userId }, transaction);
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error("Error deleting from database: " + ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error connecting to database: " + ex.Message);
                return false;
            }
        }
    }
}

// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------

public class SqliteContactSetter(string connectionString) : IContactSetter
{
    private readonly string _connectionString = connectionString;

    public void SetContactStatus(long SenderTelegramID, long AccepterTelegramID, string status)
    {
        const string query = @"
            UPDATE Contacts 
            SET Status = @Status 
            WHERE UserId = @UserId AND ContactId = @ContactId";
            
        SqliteContactGetter contactGetter = new(_connectionString);
        SqliteUserGetter userGetter = new(_connectionString);

        using (var connection = new SqliteConnection(_connectionString))
        {
            try
            {
                connection.Execute(query, new 
                {
                    Status = status,
                    UserId = userGetter.GetUserIDbyTelegramID(SenderTelegramID),
                    ContactId = contactGetter.GetContactByTelegramID(AccepterTelegramID)
                });
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
            }
        }
    }
}

public class SqliteContactGetter(string connectionString) : IContactGetter
{
    private readonly string _connectionString = connectionString;

    public async Task<List<long>> GetAllContactUserTGIds(int userId)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            SqliteUserGetter userGetter = new(_connectionString);
            
            var results = await connection.QueryAsync<(long UserId, long ContactId)>(
                @"SELECT UserId, ContactId
                FROM Contacts
                WHERE (ContactId = @UserId OR UserId = @UserId) 
                AND status = @Status",
                new { UserId = userId, Status = ContactsStatus.ACCEPTED });

            var contactUserIds = results
                .SelectMany(row => new[] { row.UserId, row.ContactId })
                .Where(id => id != userId)
                .Distinct()
                .ToList();

            return contactUserIds
                .Select(contactUserId => userGetter.GetTelegramIDbyUserID((int)contactUserId))
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving contact user IDs");
            return new List<long>();
        }
    }

    public string GetActiveMuteTimeByContactID(int contactID)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            
            var expirationDate = connection.QueryFirstOrDefault<DateTime?>(
                @"SELECT ExpirationDate 
                FROM MutedContacts 
                WHERE MutedContactId = @contactID 
                AND IsActive = 1",
                new { contactID });

            return expirationDate?.ToString("yyyy-MM-dd HH:mm:ss") 
                ?? Config.GetResourceString("NoActiveMute");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetActiveMuteTimeByContactID));
            return "";
        }
    }

    public int GetContactIDByLink(string link)
    {
        const string query = "SELECT ID FROM User WHERE Link = @link";
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            var result = connection.QueryFirstOrDefault<int?>(query, new { link });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetContactIDByLink));
            return -1;
        }
    }

    public int GetContactByTelegramID(long telegramID)
    {
        const string query = "SELECT ID FROM User WHERE TelegramID = @telegramID";
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            var result = connection.QueryFirstOrDefault<int?>(query, new { telegramID });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetContactByTelegramID));
            return -1;
        }
    }
}