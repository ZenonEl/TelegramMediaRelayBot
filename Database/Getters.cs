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
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Repositories.MySql;


namespace DataBase;

public class DBforGetters
{
    static UserGettersRepository repo = new UserGettersRepository(CoreDB.connectionString);

    private static string GetLink(long telegramID)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT Link FROM Users WHERE TelegramID = @telegramID";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@telegramID", telegramID);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetString("Link");
                }
                return "";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetLink));
                return "";
            }
        }
    }

    public static string GetSelfLink(long telegramID)
    {
        if (CoreDB.CheckExistsUser(telegramID))
        {
            return GetLink(telegramID);
        }
        return "";
    }

    public static long GetTelegramIdByLink(string link)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT TelegramID FROM Users WHERE Link = @link";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@link", link);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetInt64(reader.GetOrdinal("TelegramID"));
                }
                return -1;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetTelegramIdByLink));
                return -1;
            }
        }
    }

    //Временная обертка
    public static string GetUserNameByID(int UserID)
    {
        return repo.GetUserNameByID(UserID);
    }

    //Временная обертка
    public static long GetTelegramIDbyUserID(int UserID)
    {
        return repo.GetTelegramIDbyUserID(UserID);
    }

    //Временная обертка
    public static int GetUserIDbyTelegramID(long TelegramID)
    {
        return repo.GetUserIDbyTelegramID(TelegramID);
    }

    public static int GetContactIDByLink(string link)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT * FROM Users WHERE Link = @link";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@link", link);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetInt32("ID");
                }
                return -1;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetContactIDByLink));
                return -1;
            }
        }
    }

    public static int GetContactByTelegramID(long telegramID)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT * FROM Users WHERE TelegramID = @telegramID";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@telegramID", telegramID);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetInt32("ID");
                }
                return -1;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetContactByTelegramID));
                return -1;
            }
        }
    }

    //Временная обертка
    public static string GetUserNameByTelegramID(long telegramID)
    {
        return repo.GetUserNameByTelegramID(telegramID);
    }

    //Временная обертка
    public static List<long> GetUsersIdForMuteContactId(int contactId)
    {
        return repo.GetUsersIdForMuteContactId(contactId);
    }

    public static List<int> GetExpiredMutes()
    {
        string query = @"
            SELECT MutedId 
            FROM MutedContacts 
            WHERE ExpirationDate <= NOW() 
            AND IsActive = 1;";

        List<int> expiredMuteIds = new List<int>();

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int mutedId = reader.GetInt32("MutedId");
                    expiredMuteIds.Add(mutedId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetExpiredMutes));
            }
        }
        return expiredMuteIds;
    }

    public static string GetActiveMuteTimeByContactID(int contactID)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT ExpirationDate FROM MutedContacts WHERE MutedContactId = @contactID AND IsActive = 1";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@contactID", contactID);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetDateTime("ExpirationDate").ToString("yyyy-MM-dd HH:mm:ss");
                }
                return Config.GetResourceString("NoActiveMute");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetActiveMuteTimeByContactID));
                return "";
            }
        }
    }

    public static string GetDefaultActionByUserIDAndType(int userID, string type)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT Action, ActionCondition
            FROM DefaultUsersActions
            WHERE UserID = @userID
            AND Type = @type
            AND IsActive = 1";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userID", userID);
                command.Parameters.AddWithValue("@type", type);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    string action = reader.GetString("Action");
                    string condition = reader.GetString("ActionCondition");
                    return $"{action};{condition}";
                }

                return UsersAction.NO_VALUE;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetDefaultActionByUserIDAndType));
                return "";
            }
        }
    }
}

