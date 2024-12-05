using Microsoft.Data.SqlClient;
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
        string query = "CREATE DATABASE TikTokMediaRelayBot IF NOT EXISTS;";

        Utils.executeQuery(query);
    }
    private static void CreateUsersTable()   
    {
        string query = @"USE TikTokMediaRelayBot;
                        CREATE TABLE IF NOT EXISTS Users (
                            ID INT PRIMARY KEY IDENTITY,
                            Name VARCHAR(255) NOT NULL,
                            Link VARCHAR(255) NOT NULL,
                            Status VARCHAR(255)
                        )";

        Utils.executeQuery(query);
    }
    private static void CreateContactsTable()
    {
        string query = @"USE TikTokMediaRelayBot;
                        CREATE TABLE IF NOT EXISTS Contacts (
                        UserId INT,
                        ContactId INT,
                        PRIMARY KEY (UserId, ContactId),
                        FOREIGN KEY (UserId) REFERENCES Users(ID),
                        FOREIGN KEY (ContactId) REFERENCES Users(ID)
                        )";

        Utils.executeQuery(query);
    }
}



// При первом запуске бота создается бд
// Проверяется есть ли человек в бд 
// если нет то вносятся данные и создается уникальная ссылка для него
// если есть то просто ничего не делается
// потом тот кто хочет добавить кого то к себе в контакты должен отправить ссылку человека которого он хочет добавить в контакты
// потом бот ищет по бд этого человека по ссылке и если находит то пишет этому человеку что его хотят добавить в контакты
// если не находит то уведомляет об этом как и в случае если не захотели добавлять