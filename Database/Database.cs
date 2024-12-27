using MySql.Data.MySqlClient;
using Serilog;
using TelegramMediaRelayBot;

namespace DataBase;

public class CoreDB
{
    public static string connectionString = Config.sqlConnectionString;

    public static void initDB()
    {
        CreateDatabase();
        CreateUsersTable();
        CreateContactsTable();
        CreateMutedContactsTable();
    }

    private static void CreateDatabase()
    {
        string query = $"CREATE DATABASE IF NOT EXISTS {Config.databaseName};";
        Utils.executeVoidQuery(query);
    }

    private static void CreateUsersTable()
    {
        string query = @$"
            USE {Config.databaseName};
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
        string query = @$"
            USE {Config.databaseName};
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

    private static void CreateMutedContactsTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS MutedContacts (
                MutedId INT PRIMARY KEY AUTO_INCREMENT,
                MutedByUserId INT NOT NULL,
                MutedContactId INT NOT NULL,
                MuteDate DATETIME NOT NULL,
                ExpirationDate DATETIME NULL,
                IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                UNIQUE (MutedByUserId, MutedContactId),
                FOREIGN KEY (MutedByUserId) REFERENCES Users(ID),
                FOREIGN KEY (MutedContactId) REFERENCES Users(ID)
            )";

        Utils.executeVoidQuery(query);
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
                Log.Error("Error creating database: " + ex.Message);
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

    public static void AddContact(long telegramID, string link)
    {
        string query = @$"
            USE {Config.databaseName};
            INSERT INTO Contacts (UserId, ContactId, Status) VALUES (@userId, @contactId, @status)";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", DBforGetters.GetContactByTelegramID(telegramID));
                command.Parameters.AddWithValue("@contactId", DBforGetters.GetContactIDByLink(link));
                command.Parameters.AddWithValue("@status", ContactsStatus.WaitingForAccept);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Error creating database: " + ex.Message);
            }
        }
    }

    public static bool AddMutedContact(int mutedByUserId, int mutedContactId, DateTime? expirationDate = null, DateTime muteDate = default)
    {
        if (muteDate == default)
        {
            muteDate = DateTime.Now;
        }

        string query = @$"
            USE {Config.databaseName};
            INSERT INTO MutedContacts (MutedByUserId, MutedContactId, MuteDate, ExpirationDate)
            VALUES (@mutedByUserId, @mutedContactId, @muteDate, @expirationDate)
            ON DUPLICATE KEY UPDATE
                MuteDate = @muteDate,
                ExpirationDate = @expirationDate,
                IsActive = 1";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@mutedByUserId", mutedByUserId);
                command.Parameters.AddWithValue("@mutedContactId", mutedContactId);
                command.Parameters.AddWithValue("@muteDate", muteDate);
                command.Parameters.AddWithValue("@expirationDate", (object)expirationDate ?? DBNull.Value);

                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error creating database: " + ex.Message);
                return false;
            }
        }
    }

    public static void UnMutedContact(int userId, int contactId)
    {
        string query = @"
            UPDATE MutedContacts
            SET IsActive = 0
            WHERE MutedByUserId = @userId AND MutedContactId = @contactId";

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
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(UnMutedContact));
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
            WHERE ContactId = @UserId AND status = '{ContactsStatus.Accepted}' OR UserId = @UserId AND status = '{ContactsStatus.Accepted}'";

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
                Log.Error("Error retrieving contact user IDs: " + ex.Message);
            }
        }

        return TelegramIDs;
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
}
