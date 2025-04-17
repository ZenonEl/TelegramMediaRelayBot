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

public class DBforDefaultActions
{
    public static bool SetAutoSendVideoConditionToUser(int userId, string actionCondition, string type)
    {
        string query = @$"
            USE {Config.databaseName};
            INSERT INTO DefaultUsersActions (UserId, Type, ActionCondition) VALUES (@userId, @type, @actionCondition)
            ON DUPLICATE KEY UPDATE
                ActionCondition = @actionCondition";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@type", type);
                command.Parameters.AddWithValue("@actionCondition", actionCondition);
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

    public static bool SetAutoSendVideoActionToUser(int userId, string action, string type)
    {
        string query = @$"
            USE {Config.databaseName};
            INSERT INTO DefaultUsersActions (UserId, Type, Action) VALUES (@userId, @type, @action)
            ON DUPLICATE KEY UPDATE
                Action = @action";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@type", type);
                command.Parameters.AddWithValue("@action", action);
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

    public static bool AddDefaultUsersActionTargets(int userId, int actionId, string targetType, int targetId)
    {
        string query = @$"
            USE {Config.databaseName};
            INSERT INTO DefaultUsersActionTargets (UserId, ActionID, TargetType, TargetID) VALUES (@userId, @actionId, @targetType, @targetId)";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@actionId", actionId);
                command.Parameters.AddWithValue("@targetType", targetType);
                command.Parameters.AddWithValue("@targetId", targetId);
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

    public static bool RemoveAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        string query = @$"
            USE {Config.databaseName};
            DELETE FROM DefaultUsersActionTargets 
            WHERE UserId = @userId AND ActionID = @actionId AND TargetType = @targetType;";

        try
        {
            using (var connection = new MySqlConnection(CoreDB.connectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@actionId", actionId);
                    command.Parameters.AddWithValue("@targetType", targetType);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on removing data: {ex.Message}");
            return false;
        }
    }

    public static List<int> GetAllDefaultUsersActionTargets(int userId, string targetType, int actionId)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT * FROM DefaultUsersActionTargets 
            WHERE UserId = @userId AND TargetType = @targetType AND ActionID = @actionId;";
        List<int> targetIds = new List<int>();
        try
        {
            using (var connection = new MySqlConnection(CoreDB.connectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@targetType", targetType);
                    command.Parameters.AddWithValue("@ActionID", actionId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            targetIds.Add(reader.GetInt32("TargetID"));
                        }
                    return targetIds;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on getting data: {ex.Message}");
            return targetIds;
        }
    }

    public static int GetDefaultActionId(int userId, string type)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT ID FROM DefaultUsersActions 
            WHERE UserId = @userId AND Type = @type;";
        try
        {
            using (var connection = new MySqlConnection(CoreDB.connectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@type", type);

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on getting data: {ex.Message}");
            return 0;
        }
    }
}