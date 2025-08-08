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
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlOutboundDBGetter : IOutboundDBGetter
{
    private readonly string _connectionString;

    public MySqlOutboundDBGetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<ButtonData>> GetOutboundButtonDataAsync(int userId)
    {
        var buttonDataList = new List<ButtonData>();
        var contactUserIds = await GetContactUserIdsAsync(userId);

        foreach (var contactUserId in contactUserIds)
        {
            var userData = await GetUserDataByUserIdAsync(contactUserId);
            if (userData != null)
            {
                buttonDataList.Add(new ButtonData { ButtonText = userData.Item1, CallbackData = "user_show_outbound_invite:" + userData.Item2 });
            }
        }

        return buttonDataList;
    }

    private async Task<List<int>> GetContactUserIdsAsync(int userId)
    {
        var contactUserIds = new List<int>();
        string queryContacts = @"
            SELECT ContactId
            FROM Contacts
            WHERE UserId = @UserId AND status = 'waiting_for_accept'";

        using (MySqlConnection connection = new MySqlConnection(_connectionString))
        {
            MySqlCommand commandContacts = new MySqlCommand(queryContacts, connection);
            commandContacts.Parameters.AddWithValue("@UserId", userId);
            await connection.OpenAsync();

            using (MySqlDataReader readerContacts = (MySqlDataReader)await commandContacts.ExecuteReaderAsync())
            {
                while (await readerContacts.ReadAsync())
                {
                    int contactUserId = readerContacts.GetInt32("ContactId");
                    contactUserIds.Add(contactUserId);
                }
            }
        }

        return contactUserIds;
    }

    private async Task<Tuple<string, string>?> GetUserDataByUserIdAsync(int userId)
    {
        string queryUsers = @"
            SELECT Name, TelegramID
            FROM Users
            WHERE ID = @userId";

        using (MySqlConnection connection = new MySqlConnection(_connectionString))
        {
            MySqlCommand commandUsers = new MySqlCommand(queryUsers, connection);
            commandUsers.Parameters.AddWithValue("@userId", userId);
            await connection.OpenAsync();

            using (MySqlDataReader readerUsers = (MySqlDataReader)await commandUsers.ExecuteReaderAsync())
            {
                if (await readerUsers.ReadAsync())
                {
                    string name = readerUsers["Name"].ToString()!;
                    string telegramId = readerUsers["TelegramID"].ToString()!;
                    return new Tuple<string, string>(name, telegramId);
                }
            }
        }

        return null;
    }
}