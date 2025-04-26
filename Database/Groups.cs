// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Data;
using MySql.Data.MySqlClient;


namespace DataBase;


public class DBforGroups
{
    public static List<int> GetGroupIDsByUserId(int userId)
    {
        string query = @$"
            SELECT ID 
            FROM UsersGroups 
            WHERE UserId = @userId";

        var groupIds = new List<int>();

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        groupIds.Add(reader.GetInt32("ID"));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetGroupIDsByUserId));
            }
        }

        return groupIds;
    }

    public static bool CheckGroupOwnership(int groupId, int userId)
    {
        string query = @$"
            SELECT COUNT(*) 
            FROM UsersGroups 
            WHERE ID = @groupId AND UserId = @userId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.Parameters.AddWithValue("@userId", userId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(CheckGroupOwnership));
            }
        }
        return false;
    }

    public static string GetGroupNameById(int groupId)
    {
        string query = @$"
            SELECT GroupName 
            FROM UsersGroups 
            WHERE ID = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString("GroupName");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetGroupNameById));
            }
        }

        return "";
    }
    public static string GetGroupDescriptionById(int groupId)
    {
        string query = @$"
            SELECT Description 
            FROM UsersGroups 
            WHERE ID = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.IsDBNull("Description") ? "" : reader.GetString("Description");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetGroupDescriptionById));
            }
        }

        return "";
    }
    public static int GetGroupMemberCount(int groupId)
    {
        string query = @$"
            SELECT COUNT(*) AS MemberCount 
            FROM GroupMembers 
            WHERE GroupId = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32("MemberCount");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetGroupMemberCount));
            }
        }

        return 0;
    }

    public static bool GetIsDefaultGroup(int groupId)
    {
        string query = @$"
            SELECT IsDefaultEnabled 
            FROM UsersGroups 
            WHERE ID = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetBoolean("IsDefaultEnabled");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetIsDefaultGroup));
            }
        }
        return false;
    }

    public static bool AddGroup(int userId, string groupName, string description)
    {
        string query = @$"
            INSERT INTO UsersGroups (UserId, GroupName, Description) 
            VALUES (@userId, @groupName, @description)";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@groupName", groupName);
                command.Parameters.AddWithValue("@description", description);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(AddGroup));
                return false;
            }
        }
    }

    public static bool SetGroupName(int groupId, string groupName)
    {
        string query = @$"
            UPDATE UsersGroups 
            SET GroupName = @groupName 
            WHERE ID = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.Parameters.AddWithValue("@groupName", groupName);
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(SetGroupName));
                return false;
            }
        }
    }

    public static bool SetGroupDescription(int groupId, string description)
    {
        string query = @$"
            UPDATE UsersGroups 
            SET Description = @description 
            WHERE ID = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.Parameters.AddWithValue("@description", description);
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(SetGroupDescription));
                return false;
            }
        }
    }

    public static bool SetIsDefaultGroup(int groupId)
    {
        string query = @$"
            UPDATE UsersGroups 
            SET IsDefaultEnabled = NOT IsDefaultEnabled 
            WHERE ID = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(SetIsDefaultGroup));
                return false;
            }
        }
    }

    public static bool DeleteGroup(int groupId)
    {
        string query = @$"
            DELETE FROM UsersGroups 
            WHERE ID = @groupId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(DeleteGroup));
                return false;
            }
        }
    }

    public static List<int> GetAllUsersIdsInGroup(int groupId)
    {
        string query = @$"
            SELECT ContactId 
            FROM GroupMembers 
            WHERE GroupId = @groupId";
        List<int> userIds = [];

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userIds.Add(reader.GetInt32("ContactId"));
                    }
                    return userIds;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetAllUsersIdsInGroup));
                return userIds;
            }
        }
    }

    public static List<int> GetDefaultEnabledGroupIds(int userId)
    {
        string query = @$"
            SELECT ID
            FROM UsersGroups
            WHERE UserId = @userId AND IsDefaultEnabled = 1";

        List<int> groupIds = [];

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        groupIds.Add(reader.GetInt32("ID"));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching group IDs");
            }
        }

        return groupIds;
    }

    public static List<int> GetAllUsersInGroup(int groupId, int userId)
    {
        string query = @$"
            SELECT ContactId
            FROM GroupMembers
            WHERE GroupId = @groupId AND UserId = @userId";

        List<int> userIds = new List<int>();

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.Parameters.AddWithValue("@userId", userId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userIds.Add(reader.GetInt32("ContactId"));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching user IDs");
            }
        }

        return userIds;
    }

    public static List<int> GetAllUsersInDefaultEnabledGroups(int userId)
    {
        List<int> groupIds = GetDefaultEnabledGroupIds(userId);
        List<int> userIds = [];

        foreach (int groupId in groupIds)
        {
            userIds.AddRange(GetAllUsersInGroup(groupId, userId));
        }

        return userIds;
    }
}
