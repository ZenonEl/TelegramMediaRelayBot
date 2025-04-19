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
using DataBase.DBCreating;
using TelegramMediaRelayBot.Database.Repositories.MySql;

namespace DataBase;

public class CoreDB
{
    public readonly static string connectionString = Config.sqlConnectionString!;

    public static void InitDB()
    {
        AllCreatingFunc.CreateDatabase();
        AllCreatingFunc.CreateUsersTable();
        AllCreatingFunc.CreateContactsTable();
        AllCreatingFunc.CreateMutedContactsTable();
        AllCreatingFunc.CreateGroupsTable();
        AllCreatingFunc.CreateGroupMembersTable();
        AllCreatingFunc.CreateDefaultUsersActions();
        AllCreatingFunc.CreateDefaultUsersActionTargets();
    }

    // Временная обёртка
    public static bool CheckExistsUser(long telegramID)
    {
        var repo = new UserRepository(connectionString);
        return repo.CheckUserExists(telegramID);
    }

    public static void UnMuteByMuteId(int muteId)
    {
        string query = @$"
            USE {Config.databaseName};
            UPDATE MutedContacts SET IsActive = 0 WHERE MutedId = @muteId";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@muteId", muteId);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(UnMuteByMuteId));
            }
        }
    }

    public static bool ReCreateSelfLink(int userId)
    {
        string newLink = Utils.GenerateUserLink();
        string query = @"
            UPDATE Users SET Link = @newLink WHERE ID = @userId";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@newLink", newLink);
                command.Parameters.AddWithValue("@userId", userId);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(ReCreateSelfLink));
                return false;
            }
        }
    }
}