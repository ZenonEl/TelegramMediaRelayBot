using System.Data;
using MySql.Data.MySqlClient;
using Serilog;
using TelegramMediaRelayBot;

namespace DataBase;


public class DBforGroups
{
    public static List<int> GetGroupIDsByUserId(int userId)
    {
        string query = @$"
            USE {Config.databaseName};
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
            USE {Config.databaseName};
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
            USE {Config.databaseName};
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
            USE {Config.databaseName};
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
            USE {Config.databaseName};
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
            USE {Config.databaseName};
            SELECT Status 
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
                        return reader.GetBoolean("Status");
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
            USE {Config.databaseName};
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
            USE {Config.databaseName};
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

    public static bool UpdateGroupDescription(int groupId, string description)
    {
        string query = @$"
            USE {Config.databaseName};
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
                Log.Error(ex, "An error occurred in the method {MethodName}", nameof(UpdateGroupDescription));
                return false;
            }
        }
    }

    public static bool SetIsDefaultGroup(int groupId)
    {
        string query = @$"
            USE {Config.databaseName};
            UPDATE UsersGroups 
            SET Status = NOT Status 
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
            USE {Config.databaseName};
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
            USE {Config.databaseName};
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
}
