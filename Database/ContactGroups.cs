using MySql.Data.MySqlClient;
using Serilog;
using TelegramMediaRelayBot;

namespace DataBase;


class DBforContactGroups
{
    public static string connectionString = Config.sqlConnectionString!;

    public static bool AddContactToGroup(int userId, int contactId, int groupId)
    {
        string query = @"
            INSERT IGNORE INTO GroupMembers (UserId, ContactId, GroupId)
            VALUES (@userId, @contactId, @groupId);";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@contactId", contactId);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding contact to group");
                return false;
            }
        }
    }

    public static bool RemoveContactFromGroup(int userId, int contactId, int groupId)
    {
        string query = @"
            DELETE FROM GroupMembers
            WHERE UserId = @userId AND ContactId = @contactId AND GroupId = @groupId";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@contactId", contactId);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing contact from group");
                return false;
            }
        }
    }

    public static bool CheckUserAndContactConnect(int userId, int contactId)
    {
        string query = @$"
            SELECT COUNT(*) FROM Contacts
            WHERE ((UserId = @userId AND ContactId = @contactId) OR (UserId = @contactId AND ContactId = @userId)) AND Status = '{Types.ContactsStatus.ACCEPTED}'";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@contactId", contactId);
                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking user and contact connection");
                return false;
            }
        }
    }
}