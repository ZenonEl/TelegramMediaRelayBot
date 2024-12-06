using MySql.Data.MySqlClient;
using TikTokMediaRelayBot;

namespace DataBase;

public class Database
{
    public static string connectionString = Config.sqlConnectionString;

    public static void initDB()
    {
        CreateDatabase();
        CreateUsersTable();
        CreateContactsTable();
    }

    private static void CreateDatabase()
    {
        string query = "CREATE DATABASE IF NOT EXISTS TikTokMediaRelayBot;";
        Utils.executeVoidQuery(query);
    }

    private static void CreateUsersTable()
    {
        string query = @"
            USE TikTokMediaRelayBot;
            CREATE TABLE IF NOT EXISTS Users (
                ID INT PRIMARY KEY AUTO_INCREMENT,
                TelegramID BIGINT NOT NULL,
                Name VARCHAR(255) NOT NULL,
                Link VARCHAR(255) NOT NULL,
                Status VARCHAR(255)
            )";

        Utils.executeVoidQuery(query);
    }

    private static void CreateContactsTable()
    {
        string query = @"
            USE TikTokMediaRelayBot;
            CREATE TABLE IF NOT EXISTS Contacts (
                UserId INT,
                ContactId INT,
                status VARCHAR(255),
                PRIMARY KEY (UserId, ContactId),
                FOREIGN KEY (UserId) REFERENCES Users(ID),
                FOREIGN KEY (ContactId) REFERENCES Users(ID)
            )";

        Utils.executeVoidQuery(query);
    }

    private static bool CheckExistsUser(long telegramID)
    {
        string query = @"
            USE TikTokMediaRelayBot;
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
                Console.WriteLine("Error creating database: " + ex.Message);
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

        string query = @"
            USE TikTokMediaRelayBot;
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
                Console.WriteLine("Error inserting data to Users: " + ex.Message);
            }
        }
    }

    private static void AddContact(int userId, int contactId)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            INSERT INTO Contacts (UserId, ContactId) VALUES (@userId, @contactId)";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@contactId", contactId);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating database: " + ex.Message);
            }
        }
    }
    private static string GetLink(long telegramID)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            SELECT Link FROM Users WHERE TelegramID = @telegramID";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
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

    public static string GetSelfLink(long telegramID)
    {
        if (CheckExistsUser(telegramID))
        {
            return GetLink(telegramID);
        }
        return "";
    }
}
