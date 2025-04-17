// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using MySql.Data.MySqlClient;
using TelegramMediaRelayBot;


namespace DataBase;

public static class ContactAdder
{
    public static void AddContact(long telegramID, string link)
    {
        string query = @$"
            USE {Config.databaseName};
            INSERT INTO Contacts (UserId, ContactId, Status) VALUES (@userId, @contactId, @status)";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", DBforGetters.GetContactByTelegramID(telegramID));
                command.Parameters.AddWithValue("@contactId", DBforGetters.GetContactIDByLink(link));
                command.Parameters.AddWithValue("@status", Types.ContactsStatus.WAITING_FOR_ACCEPT);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
            }
        }
    }

    public static bool AddMutedContact(int mutedByUserId, int mutedContactId, DateTime? expirationDate = null, DateTime muteDate = default)
    {
        if (muteDate == default)
        {
            muteDate = DateTime.Now;
        }

        string query = @$"
            USE {Config.databaseName};
            INSERT INTO MutedContacts (MutedByUserId, MutedContactId, MuteDate, ExpirationDate)
            VALUES (@mutedByUserId, @mutedContactId, @muteDate, @expirationDate)
            ON DUPLICATE KEY UPDATE
                MuteDate = @muteDate,
                ExpirationDate = @expirationDate,
                IsActive = 1";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@mutedByUserId", mutedByUserId);
                command.Parameters.AddWithValue("@mutedContactId", mutedContactId);
                command.Parameters.AddWithValue("@muteDate", muteDate);
                command.Parameters.AddWithValue("@expirationDate", (object)expirationDate ?? DBNull.Value);

                command.ExecuteNonQuery();
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

// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------

public static class ContactRemover
{
    public static void RemoveMutedContact(int userId, int contactId)
    {
        string query = @"
            UPDATE MutedContacts
            SET IsActive = 0
            WHERE MutedByUserId = @userId AND MutedContactId = @contactId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@contactId", contactId);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(RemoveMutedContact));
            }
        }
    }

    public static bool RemoveContactByStatus(int senderTelegramID, int accepterTelegramID, string? status = null)
    {
        string query = @$"
            USE {Config.databaseName};
            DELETE FROM Contacts
            WHERE (UserId = @senderTelegramID AND ContactId = @accepterTelegramID AND (@status IS NULL OR Status = @status))
            OR (UserId = @accepterTelegramID AND ContactId = @senderTelegramID AND (@status IS NULL OR Status = @status))";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@senderTelegramID", senderTelegramID);
                command.Parameters.AddWithValue("@accepterTelegramID", accepterTelegramID);
                command.Parameters.AddWithValue("@status", (object)status ?? DBNull.Value);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
                return false;
            }
        }
    }

    public static bool RemoveUsersFromContacts(int userId, List<int> contactIds)
    {
        if (contactIds == null || contactIds.Count == 0)
        {
            return true;
        }

        string contactIdParams = string.Join(", ", contactIds.Select((_, i) => $"@contactId{i}"));

        string deleteContactsQuery = @$"
            USE {Config.databaseName};
            DELETE FROM Contacts
            WHERE (UserId = @userId AND ContactId IN ({contactIdParams}))
            OR (ContactId = @userId AND UserId IN ({contactIdParams}));";

        string deleteGroupMembersQuery = @$"
            USE {Config.databaseName};
            DELETE FROM GroupMembers
            WHERE (UserId = @userId AND ContactId IN ({contactIdParams}))
            OR (ContactId = @userId AND UserId IN ({contactIdParams}));";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        MySqlCommand deleteContactsCommand = new MySqlCommand(deleteContactsQuery, connection, transaction);
                        deleteContactsCommand.Parameters.AddWithValue("@userId", userId);

                        for (int i = 0; i < contactIds.Count; i++)
                        {
                            deleteContactsCommand.Parameters.AddWithValue($"@contactId{i}", contactIds[i]);
                        }

                        deleteContactsCommand.ExecuteNonQuery();

                        MySqlCommand deleteGroupMembersCommand = new MySqlCommand(deleteGroupMembersQuery, connection, transaction);
                        deleteGroupMembersCommand.Parameters.AddWithValue("@userId", userId);

                        for (int i = 0; i < contactIds.Count; i++)
                        {
                            deleteGroupMembersCommand.Parameters.AddWithValue($"@contactId{i}", contactIds[i]);
                        }

                        deleteGroupMembersCommand.ExecuteNonQuery();

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

    public static bool RemoveAllContactsExcept(int userId, List<int> excludeIds)
    {
        if (excludeIds == null || excludeIds.Count == 0)
        {
            return RemoveAllContacts(userId);
        }

        string excludeParams = string.Join(",", excludeIds.Select((_, i) => $"@excludeId{i}"));

        string deleteContactsQuery = @$"
            USE {Config.databaseName};
            DELETE FROM Contacts
            WHERE (UserId = @userId AND ContactId NOT IN ({excludeParams}))
            OR (ContactId = @userId AND UserId NOT IN ({excludeParams}));";

        string deleteGroupMembersQuery = @$"
            USE {Config.databaseName};
            DELETE FROM GroupMembers
            WHERE (UserId = @userId AND ContactId NOT IN ({excludeParams}))
            OR (ContactId = @userId AND UserId NOT IN ({excludeParams}));";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        MySqlCommand deleteContactsCommand = new MySqlCommand(deleteContactsQuery, connection, transaction);
                        deleteContactsCommand.Parameters.AddWithValue("@userId", userId);
                        
                        for (int i = 0; i < excludeIds.Count; i++)
                        {
                            deleteContactsCommand.Parameters.AddWithValue($"@excludeId{i}", excludeIds[i]);
                        }
                        
                        deleteContactsCommand.ExecuteNonQuery();

                        MySqlCommand deleteGroupMembersCommand = new MySqlCommand(deleteGroupMembersQuery, connection, transaction);
                        deleteGroupMembersCommand.Parameters.AddWithValue("@userId", userId);
                        
                        for (int i = 0; i < excludeIds.Count; i++)
                        {
                            deleteGroupMembersCommand.Parameters.AddWithValue($"@excludeId{i}", excludeIds[i]);
                        }
                        
                        deleteGroupMembersCommand.ExecuteNonQuery();

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

    public static bool RemoveAllContacts(int userId)
    {
        string deleteContactsQuery = @$"
            USE {Config.databaseName};
            DELETE FROM Contacts
            WHERE UserId = @userId OR ContactId = @userId;";

        string deleteGroupMembersQuery = @$"
            USE {Config.databaseName};
            DELETE FROM GroupMembers
            WHERE UserId = @userId OR ContactId = @userId;";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        MySqlCommand deleteContactsCommand = new MySqlCommand(deleteContactsQuery, connection, transaction);
                        deleteContactsCommand.Parameters.AddWithValue("@userId", userId);
                        deleteContactsCommand.ExecuteNonQuery();

                        MySqlCommand deleteGroupMembersCommand = new MySqlCommand(deleteGroupMembersQuery, connection, transaction);
                        deleteGroupMembersCommand.Parameters.AddWithValue("@userId", userId);
                        deleteGroupMembersCommand.ExecuteNonQuery();

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

public static class ContactSetter
{
    public static void SetContactStatus(long SenderTelegramID, long AccepterTelegramID, string status)
    {
        string query = @$"
            USE {Config.databaseName};
            UPDATE Contacts SET Status = @Status WHERE UserId = @UserId AND ContactId = @ContactId";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@UserId", DBforGetters.GetUserIDbyTelegramID(SenderTelegramID));
                command.Parameters.AddWithValue("@ContactId", DBforGetters.GetContactByTelegramID(AccepterTelegramID));
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
            }
        }
    }
}

public static class ContactGetter
{
    public static async Task<List<long>> GetAllContactUserTGIds(int userId)
    {
        List<long> contactUserIds = new List<long>();
        List<long> TelegramIDs = new List<long>();
        string query = @$"
            SELECT UserId, ContactId
            FROM Contacts
            WHERE (ContactId = @UserId OR UserId = @UserId) AND status = '{Types.ContactsStatus.ACCEPTED}'";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                await connection.OpenAsync();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (await reader.ReadAsync())
                    {
                        long contactUserId = reader.GetInt64("UserId");
                        long contactId = reader.GetInt64("ContactId");
                        if (contactUserId != userId) contactUserIds.Add(contactUserId);
                        if (userId != contactId) contactUserIds.Add(contactId);
                    }
                    foreach (var contactUserId in contactUserIds)
                    {
                        TelegramIDs.Add(DBforGetters.GetTelegramIDbyUserID((int)contactUserId));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error retrieving contact user IDs: " + ex.Message);
            }
        }

        return TelegramIDs;
    }
}