using MySql.Data.MySqlClient;  // Импортируем библиотеку для работы с MySQL
using TikTokMediaRelayBot;

namespace DataBase
{
    public class Utils
    {
        public static void executeVoidQuery(string query)
        {
            string connectionString = Config.sqlConnectionString;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating database: " + ex.Message);
                }
            }
        }
    }
}
