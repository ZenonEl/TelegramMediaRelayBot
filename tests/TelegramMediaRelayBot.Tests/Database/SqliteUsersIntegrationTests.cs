using FluentAssertions;
using Microsoft.Data.Sqlite;
using Dapper;
using TelegramMediaRelayBot.Database.Repositories.Sqlite;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Tests.Database;

public class SqliteUsersIntegrationTests
{
    private static string CS => "Data Source=:memory:";

    private static void ApplySchema(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Users (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                TelegramID INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Link TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS MutedContacts (
                MutedId INTEGER PRIMARY KEY AUTOINCREMENT,
                MutedByUserId INTEGER NOT NULL,
                MutedContactId INTEGER NOT NULL,
                ExpirationDate TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );
        ");
    }

    [Fact]
    public async Task UserRepository_Add_Recreate_SelfLink_And_Getters_Work()
    {
        using var conn = new SqliteConnection(CS);
        await conn.OpenAsync();
        ApplySchema(conn);

        IUserRepository repo = new SqliteUserRepository(CS);
        IUserGetter getter = new SqliteUserGetter(CS);

        // Add user
        repo.AddUser("user1", 12345, user: false);

        // Check exists
        repo.CheckUserExists(12345).Should().BeTrue();

        // Get user id
        var userId = getter.GetUserIDbyTelegramID(12345);
        userId.Should().BeGreaterThan(0);

        // Get by link
        var link = conn.ExecuteScalar<string>("SELECT Link FROM Users WHERE ID=@id", new { id = userId });
        (await getter.GetUserTelegramIdByLinkAsync(link)).Should().Be(12345);

        // Recreate link
        repo.ReCreateUserSelfLink(userId).Should().BeTrue();
        var newLink = conn.ExecuteScalar<string>("SELECT Link FROM Users WHERE ID=@id", new { id = userId });
        newLink.Should().NotBeNullOrEmpty().And.NotBe(link);
    }
}

