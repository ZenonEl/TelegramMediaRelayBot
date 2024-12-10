using MySql.Data.MySqlClient;  // Импортируем библиотеку для работы с MySQL
using Serilog;
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
                    Log.Error(ex, "Произошла ошибка в методе {MethodName}", nameof(executeVoidQuery));
                }
            }
        }
        public static string GenerateUserLink()
        {
            return Guid.NewGuid().ToString();
        }
    }

}
