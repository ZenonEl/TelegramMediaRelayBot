using MySql.Data.MySqlClient;
using TikTokMediaRelayBot;

namespace DataBase;


public class DBforInbounds
{
    public static List<ButtonData> GetButtonDataFromDatabase(int userId)
    {
        var buttonDataList = new List<ButtonData>();
        var contactUserIds = GetContactUserIds(userId);

        foreach (var contactUserId in contactUserIds)
        {
            var userData = GetUserData(contactUserId);
            if (userData != null)
            {
                buttonDataList.Add(new ButtonData { ButtonText = userData.Item1, CallbackData = "user_accept_inbounds_invite:" + userData.Item2 });
            }
        }

        return buttonDataList;
    }

    private static List<int> GetContactUserIds(int userId)
    {
        var contactUserIds = new List<int>();
        string queryContacts = @"
            SELECT UserId
            FROM Contacts
            WHERE ContactId = @UserId AND status = 'waiting_for_accept'";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            MySqlCommand commandContacts = new MySqlCommand(queryContacts, connection);
            commandContacts.Parameters.AddWithValue("@UserId", userId);
            connection.Open();

            using (MySqlDataReader readerContacts = commandContacts.ExecuteReader())
            {
                while (readerContacts.Read())
                {
                    int contactUserId = readerContacts.GetInt32("UserId");
                    contactUserIds.Add(contactUserId);
                }
            }
        }

        return contactUserIds;
    }

    private static Tuple<string, string>? GetUserData(int contactUserId)
    {
        string queryUsers = @"
            SELECT Name, TelegramID
            FROM Users
            WHERE ID = @ContactUserId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            MySqlCommand commandUsers = new MySqlCommand(queryUsers, connection);
            commandUsers.Parameters.AddWithValue("@ContactUserId", contactUserId);
            connection.Open();

            using (MySqlDataReader readerUsers = commandUsers.ExecuteReader())
            {
                if (readerUsers.Read())
                {
                    string name = readerUsers["Name"].ToString()!;
                    string telegramId = readerUsers["TelegramID"].ToString()!;
                    return new Tuple<string, string>(name, telegramId);
                }
            }
        }

        return null;
    }

    public static void SetContactStatus(long SenderTelegramID, long AccepterTelegramID, string status)
    {
        string query = @"
            USE TikTokMediaRelayBot;
            UPDATE Contacts SET Status = @Status WHERE UserId = @UserId AND ContactId = @ContactId";
        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
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
                Console.WriteLine("Error creating database: " + ex.Message);
            }
        }
    }
}
