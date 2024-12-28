using DataBase.Types;
using MySql.Data.MySqlClient;

namespace DataBase;


public class DBforInbounds
{
    public static List<ButtonData> GetInboundsButtonData(int userId)
    {
        var buttonDataList = new List<ButtonData>();
        var contactUserIds = GetContactUserIds(userId);

        foreach (var contactUserId in contactUserIds)
        {
            var userData = GetUserDataByContactId(contactUserId);
            if (userData != null)
            {
                buttonDataList.Add(new ButtonData { ButtonText = userData.Item1, CallbackData = "user_show_inbounds_invite:" + userData.Item2 });
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

    private static Tuple<string, string>? GetUserDataByContactId(int contactId)
    {
        string queryUsers = @"
            SELECT Name, TelegramID
            FROM Users
            WHERE ID = @contactId";

        using (MySqlConnection connection = new MySqlConnection(CoreDB.connectionString))
        {
            MySqlCommand commandUsers = new MySqlCommand(queryUsers, connection);
            commandUsers.Parameters.AddWithValue("@contactId", contactId);
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
}
