using MySql.Data.MySqlClient;
using Serilog;
using TelegramMediaRelayBot;
using DataBase.DBCreating;

namespace DataBase;

public class CoreDB
{
    public static string connectionString = Config.sqlConnectionString!;

    public static void initDB()
    {
        AllCreatingFunc.CreateDatabase();
        AllCreatingFunc.CreateUsersTable();
        AllCreatingFunc.CreateContactsTable();
        AllCreatingFunc.CreateMutedContactsTable();
        AllCreatingFunc.CreateGroupsTable();
        AllCreatingFunc.CreateGroupMembersTable();
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
                command.Parameters.AddWithValue("@status", Types.ContactsStatus.WAITING_FOR_ACCEPT);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
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
                Log.Error("Error editing database: " + ex.Message);
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
            WHERE ContactId = @UserId AND status = '{Types.ContactsStatus.ACCEPTED}' OR UserId = @UserId AND status = '{Types.ContactsStatus.ACCEPTED}'";

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

    public static void SetContactStatus(long SenderTelegramID, long AccepterTelegramID, string status)
    {
        string query = @$"
            USE {Config.databaseName};
            UPDATE Contacts SET Status = @Status WHERE UserId = @UserId AND ContactId = @ContactId";
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@UserId", DBforGetters.GetUserIDbyTelegramID(SenderTelegramID));
                command.Parameters.AddWithValue("@ContactId", DBforGetters.GetContactByTelegramID(AccepterTelegramID));
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error("Error editing database: " + ex.Message);
            }
        }
    }
}
