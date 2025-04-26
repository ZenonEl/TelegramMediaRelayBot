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
using TelegramMediaRelayBot.Database.Repositories.MySql;


namespace DataBase;

public class DBforGetters
{
    static MySqlUserGetter repo = new MySqlUserGetter(CoreDB.connectionString);

    private static string GetUserLink(long telegramID)
    {
        string query = @$"
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
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetUserLink));
                return "";
            }
        }
    }

    public static string GetUserSelfLink(long telegramID)
    {
        if (CoreDB.CheckExistsUser(telegramID))
        {
            return GetUserLink(telegramID);
        }
        return "";
    }

    public static long GetUserTelegramIdByLink(string link)
    {
        string query = @$"
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
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetUserTelegramIdByLink));
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
}

