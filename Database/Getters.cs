using System.Globalization;
using MySql.Data.MySqlClient;
using Serilog;
using TikTokMediaRelayBot;

namespace DataBase;

public class DBforGetters
{
    private static string GetLink(long telegramID)
    {
        string query = @"
            USE TikTokMediaRelayBot;
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

    public static string GetUserNameByID(int UserID)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            SELECT Name FROM Users WHERE ID = @UserID";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", UserID);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetString("Name");
                }
                return "";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetUserNameByID));
                return "Not found";
            }
        }
    }

    public static long GetTelegramIDbyUserID(int UserID)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            SELECT TelegramID FROM Users WHERE ID = @UserID";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", UserID);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetInt64(reader.GetOrdinal("TelegramID"));
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetTelegramIDbyUserID));
                return -1;
            }
        }
    }

    public static int GetUserIDbyTelegramID(long TelegramID)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            SELECT ID FROM Users WHERE TelegramID = @TelegramID";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TelegramID", TelegramID);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetInt32("ID");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetUserIDbyTelegramID));
                return -1;
            }
        }
    }
    public static int GetContactIDByLink(string link)
    {
        string query = @"
            USE TikTokMediaRelayBot;
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
        string query = @"
            USE TikTokMediaRelayBot;
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
    public static async Task<string> GetUserNameByTelegramID(long telegramID)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            SELECT Name FROM Users WHERE TelegramID = @telegramID";
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
                    return reader.GetString("Name");
                }
                return "";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetUserNameByTelegramID));
                return "Not found";
            }
        }
    }
    public static List<long> GetUsersIdForMuteContactId(int contactId)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            SELECT MutedByUserId FROM MutedContacts WHERE MutedContactId = @contactId AND IsActive = 1";
        
        List<int> mutedByUserIds = new List<int>();
        var TelegramIDs = new List<long>();

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@contactId", contactId);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    mutedByUserIds.Add(reader.GetInt32("MutedByUserId"));
                }
                foreach (var contactUserId in mutedByUserIds)
                {
                    TelegramIDs.Add(GetTelegramIDbyUserID(contactUserId));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetUsersIdForMuteContactId));
            }
        }

        return TelegramIDs;
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
        string query = @"
            USE TikTokMediaRelayBot;
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
                return Config.resourceManager.GetString("NoActiveMute", CultureInfo.CurrentUICulture);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(GetActiveMuteTimeByContactID));
                return "";
            }
        }
    }
}

