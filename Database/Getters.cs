using MySql.Data.MySqlClient;
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
                Console.WriteLine("Error creating database: " + ex.Message);
                return "";
            }
        }
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
                Console.WriteLine("Error creating database: " + ex.Message);
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
                Console.WriteLine("Error creating database: " + ex.Message);
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
                Console.WriteLine("Error creating database: " + ex.Message);
                return -1;
            }
        }
    }
    public static int GetContactByLink(string link)
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
                Console.WriteLine("Error creating database: " + ex.Message);
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
                Console.WriteLine("Error creating database: " + ex.Message);
                return -1;
            }
        }
    }
    public static string GetUserNameByTelegramID(long telegramID)
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
                Console.WriteLine("Error creating database: " + ex.Message);
                return "Not found";
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
}
