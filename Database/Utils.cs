using MySql.Data.MySqlClient;
using Serilog;
using TelegramMediaRelayBot;

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
                    Log.Error(ex, "An error occurred in the method{MethodName}", nameof(executeVoidQuery));
                }
            }
        }
        public static string GenerateUserLink()
        {
            return Guid.NewGuid().ToString();
        }
    }

}
