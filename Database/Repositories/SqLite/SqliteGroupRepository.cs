using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteGroupRepository(IDbConnection dbConnection) : IGroupRepository
{
    public Task<int> CreateGroup(int userId, string groupName, string description)
    {
        const string query = "INSERT INTO UsersGroups (UserId, GroupName, Description) VALUES (@userId, @groupName, @description)";
        return dbConnection.ExecuteAsync(query, new { userId, groupName, description });
    }

    public Task<int> UpdateGroupName(int groupId, string groupName)
    {
        const string query = "UPDATE UsersGroups SET GroupName = @groupName WHERE ID = @groupId";
        return dbConnection.ExecuteAsync(query, new { groupId, groupName });
    }

    public Task<int> UpdateGroupDescription(int groupId, string description)
    {
        const string query = "UPDATE UsersGroups SET Description = @description WHERE ID = @groupId";
        return dbConnection.ExecuteAsync(query, new { groupId, description });
    }

    public Task<int> ToggleDefaultStatus(int groupId)
    {
        const string query = "UPDATE UsersGroups SET IsDefaultEnabled = NOT IsDefaultEnabled WHERE ID = @groupId";
        return dbConnection.ExecuteAsync(query, new { groupId });
    }

    public Task<int> DeleteGroup(int groupId)
    {
        const string query = "DELETE FROM UsersGroups WHERE ID = @groupId";
        return dbConnection.ExecuteAsync(query, new { groupId });
    }
}