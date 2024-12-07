using MySql.Data.MySqlClient;
using TikTokMediaRelayBot;

namespace DataBase;

public class CoreDB
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

    public static bool CheckExistsUser(long telegramID)
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

    public static async Task<List<long>> GetContactUserTGIds(int userId)
    {
        var contactUserIds = new List<long>();
        var TelegramIDs = new List<long>();
        string query = @$"
            SELECT UserId, ContactId
            FROM Contacts
            WHERE ContactId = @UserId OR UserId = @UserId AND status = '{ContactsStatus.Accepted}'";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                await connection.OpenAsync();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (await reader.ReadAsync())
                    {
                        long contactUserId = reader.GetInt64("UserId");
                        long contactId = reader.GetInt64("ContactId");
                        if (contactUserId != userId) contactUserIds.Add(contactUserId);
                        if (userId != contactId) contactUserIds.Add(contactId);
                    }
                    foreach (var contactUserId in contactUserIds)
                    {
                        TelegramIDs.Add(DBforGetters.GetTelegramIDbyUserID((int)contactUserId));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving contact user IDs: " + ex.Message);
            }
        }

        return TelegramIDs;
    }

    public static void AddContact(long telegramID, string link)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            INSERT INTO Contacts (UserId, ContactId, Status) VALUES (@userId, @contactId, @status)";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", DBforGetters.GetContactByTelegramID(telegramID));
                command.Parameters.AddWithValue("@contactId", DBforGetters.GetContactByLink(link));
                command.Parameters.AddWithValue("@status", ContactsStatus.WaitingForAccept);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating database: " + ex.Message);
            }
        }
    }
}
