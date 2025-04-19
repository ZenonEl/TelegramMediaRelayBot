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

public static class PrivacySettingsSetter
{
    public static bool SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        string query = @$"
            USE {Config.databaseName};
            INSERT INTO PrivacySettings (UserId, Type, Action, IsActive, ActionCondition)
            VALUES (@userId, @type, @action, @isActive, @actionCondition)
            ON DUPLICATE KEY UPDATE
            Action = @action,
            IsActive = @isActive,
            ActionCondition = @actionCondition";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@type", type);
                command.Parameters.AddWithValue("@action", action);
                command.Parameters.AddWithValue("@isActive", isActive);
                command.Parameters.AddWithValue("@actionCondition", actionCondition);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting privacy rule");
                return false;
            }
        }
    }

    public static bool SetPrivacyRuleToDisabled(int userId, string type)
    {
        string query = @"
            UPDATE PrivacySettings
            SET IsActive = 0
            WHERE UserId = @userId AND Type = @type";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@type", type);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting privacy rule to disabled");
                return false;
            }
        }
    }
}
// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------

public static class PrivacySettingsGetter
{
    public static bool GetIsActivePrivacyRule(int userId, string type)
    {
        string query = @"
            SELECT IsActive
            FROM PrivacySettings
            WHERE UserId = @userId AND Type = @type";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@type", type);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetBoolean("IsActive");
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting privacy rule");
                return false;
            }
        }
    }
}
