// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using DataBase.Types;
using MySql.Data.MySqlClient;
using Serilog;
using TelegramMediaRelayBot;

namespace DataBase;


public class DBforOutbound
{
    public static List<ButtonData> GetOutboundButtonData(int userId)
    {
        var buttonDataList = new List<ButtonData>();
        var contactUserIds = GetContactUserIds(userId);

        foreach (var contactUserId in contactUserIds)
        {
            var userData = GetUserDataByUserId(contactUserId);
            if (userData != null)
            {
                buttonDataList.Add(new ButtonData { ButtonText = userData.Item1, CallbackData = "user_show_outbound_invite:" + userData.Item2 });
            }
        }

        return buttonDataList;
    }

    private static List<int> GetContactUserIds(int userId)
    {
        var contactUserIds = new List<int>();
        string queryContacts = @"
            SELECT ContactId
            FROM Contacts
            WHERE UserId = @UserId AND status = 'waiting_for_accept'";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            MySqlCommand commandContacts = new MySqlCommand(queryContacts, connection);
            commandContacts.Parameters.AddWithValue("@UserId", userId);
            connection.Open();

            using (MySqlDataReader readerContacts = commandContacts.ExecuteReader())
            {
                while (readerContacts.Read())
                {
                    int contactUserId = readerContacts.GetInt32("ContactId");
                    contactUserIds.Add(contactUserId);
                }
            }
        }

        return contactUserIds;
    }

    private static Tuple<string, string>? GetUserDataByUserId(int userId)
    {
        string queryUsers = @"
            SELECT Name, TelegramID
            FROM Users
            WHERE ID = @userId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            MySqlCommand commandUsers = new MySqlCommand(queryUsers, connection);
            commandUsers.Parameters.AddWithValue("@userId", userId);
            connection.Open();

            using (MySqlDataReader readerUsers = commandUsers.ExecuteReader())
            {
                if (readerUsers.Read())
                {
                    string name = readerUsers["Name"].ToString()!;
                    string telegramId = readerUsers["TelegramID"].ToString()!;
                    return new Tuple<string, string>(name, telegramId);
                }
            }
        }

        return null;
    }

    public static void DeleteOutboundContact(long SenderTelegramID, long AccepterTelegramID, string status)
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
