using TelegramMediaRelayBot;


namespace DataBase.DBCreating;

public class AllCreatingFunc
{
    public static string connectionString = Config.sqlConnectionString!;
    public static void CreateDatabase()
    {
        string query = $"CREATE DATABASE IF NOT EXISTS {Config.databaseName};";
        Utils.executeVoidQuery(query);
    }

    public static void CreateUsersTable()
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

    public static void CreateContactsTable()
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

    public static void CreateMutedContactsTable()
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

    public static void CreateGroupsTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS UsersGroups (
                ID INT PRIMARY KEY AUTO_INCREMENT,
                UserId INT NOT NULL,
                GroupName VARCHAR(255) NOT NULL,
                Description TEXT NULL,
                Status BOOLEAN NOT NULL DEFAULT TRUE,
                UNIQUE (UserId, GroupName),
                FOREIGN KEY (UserId) REFERENCES Users(ID)
            )";

        Utils.executeVoidQuery(query);
    }

    public static void CreateGroupMembersTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS GroupMembers (
                ID INT PRIMARY KEY AUTO_INCREMENT,
                UserId INT NOT NULL,
                ContactId INT NOT NULL,
                GroupId INT NOT NULL,
                Status BOOLEAN NOT NULL DEFAULT TRUE,
                UNIQUE (GroupId, ContactId),
                FOREIGN KEY (UserId) REFERENCES Users(ID),
                FOREIGN KEY (ContactId) REFERENCES Users(ID),
                FOREIGN KEY (GroupId) REFERENCES UsersGroups(ID)
            )";

        Utils.executeVoidQuery(query);
    }
}