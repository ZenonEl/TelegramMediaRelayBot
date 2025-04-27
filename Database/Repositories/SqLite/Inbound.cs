// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Microsoft.Data.Sqlite;
using TelegramMediaRelayBot.Database.Interfaces;
using System.Data;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteInboundDBGetter : IInboundDBGetter
{
    private readonly string _connectionString;

    public SqliteInboundDBGetter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<ButtonData> GetInboundsButtonData(int userId)
    {
        var buttonDataList = new List<ButtonData>();
        var contactUserIds = GetContactUserIds(userId);

        foreach (var contactUserId in contactUserIds)
        {
            var userData = GetUserDataByContactId(contactUserId);
            if (userData != null)
            {
                buttonDataList.Add(new ButtonData { 
                    ButtonText = userData.Item1, 
                    CallbackData = "user_show_inbounds_invite:" + userData.Item2 
                });
            }
        }

        return buttonDataList;
    }

    private List<int> GetContactUserIds(int userId)
    {
        var contactUserIds = new List<int>();
        const string queryContacts = @"
            SELECT UserId
            FROM Contacts
            WHERE ContactId = @UserId AND status = 'waiting_for_accept'";

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            
            using (var command = new SqliteCommand(queryContacts, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contactUserIds.Add(reader.GetInt32(0));
                    }
                }
            }
        }

        return contactUserIds;
    }

    private Tuple<string, string>? GetUserDataByContactId(int contactId)
    {
        const string queryUsers = @"
            SELECT Name, TelegramID
            FROM Users
            WHERE ID = @contactId";

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            
            using (var command = new SqliteCommand(queryUsers, connection))
            {
                command.Parameters.AddWithValue("@contactId", contactId);
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Tuple<string, string>(
                            reader.GetString(0),
                            reader.GetString(1)
                        );
                    }
                }
            }
        }

        return null;
    }
}