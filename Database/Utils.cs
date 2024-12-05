using Microsoft.Data.SqlClient;


namespace DataBase;


public class Utils 
{
    public static void executeQuery(string query)
    {
        using (SqlConnection connection = new SqlConnection(Database.connectionString))
        {
            try
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating database: " + ex.Message);
            }
        }
    }
}