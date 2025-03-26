using MySql.Data.MySqlClient;
using Serilog;
using TelegramMediaRelayBot;
using DataBase.DBCreating;

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

    public static bool CheckExistsUser(long telegramID)
    {
        string query = @$"
            USE {Config.databaseName};
            SELECT * FROM Users WHERE TelegramID = @telegramID";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@telegramID", telegramID);
                MySqlDataReader reader = command.ExecuteReader();
                return reader.HasRows;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting data from database: " + ex.Message);
                return false;
            }
        }
    }

    public static void AddUser(string name, long telegramID)
    {
        bool user = CheckExistsUser(telegramID);
        string link = Utils.GenerateUserLink();

        if (user)
        {
            return;
        }

        string query = @$"
            USE {Config.databaseName};
            INSERT INTO Users (TelegramID, Name, Link) VALUES (@telegramID, @name, @link)";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@telegramID", telegramID);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@link", link);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Error inserting data to Users: " + ex.Message);
            }
        }
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