using FluentAssertions;
using Microsoft.Data.Sqlite;
using Dapper;
using TelegramMediaRelayBot.Database.Repositories.Sqlite;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Tests.Database;

public class SqliteIntegrationTests
{
    private static string CreateInMemoryConnectionString() => "Data Source=:memory:";

    private static void ApplySchema(SqliteConnection connection)
    {
        // Minimal schema to test repositories
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Users (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                TelegramID INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Link TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Contacts (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ContactId INTEGER NOT NULL,
                Status TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS UsersGroups (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                GroupName TEXT NOT NULL,
                Description TEXT DEFAULT '',
                IsDefaultEnabled INTEGER DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS GroupMembers (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ContactId INTEGER NOT NULL,
                GroupId INTEGER NOT NULL
            );
        ");
    }

    [Fact]
    public void ContactGroupRepository_AddRemove_Works()
    {
        var cs = CreateInMemoryConnectionString();

        using var connection = new SqliteConnection(cs);
        connection.Open();
        ApplySchema(connection);

        // Seed minimal data
        connection.Execute("INSERT INTO Users(TelegramID, Name, Link) VALUES (1,'u1','l1');");
        connection.Execute("INSERT INTO Users(TelegramID, Name, Link) VALUES (2,'u2','l2');");
        connection.Execute("INSERT INTO Contacts(UserId, ContactId, Status) VALUES (1,2,'accepted');");
        connection.Execute("INSERT INTO UsersGroups(UserId, GroupName, Description, IsDefaultEnabled) VALUES (1,'g1','',0);");
        int groupId = connection.ExecuteScalar<int>("SELECT ID FROM UsersGroups WHERE UserId=1 AND GroupName='g1'");

        IContactGroupRepository repo = new SqliteContactGroupRepository(cs);

        // Add
        var added = repo.AddContactToGroup(1, 2, groupId);
        added.Should().BeTrue();

        // Check linkage exists via repo.CheckUserAndContactConnect
        var linked = repo.CheckUserAndContactConnect(1, 2);
        linked.Should().BeTrue();

        // Remove
        var removed = repo.RemoveContactFromGroup(1, 2, groupId);
        removed.Should().BeTrue();
    }
}

