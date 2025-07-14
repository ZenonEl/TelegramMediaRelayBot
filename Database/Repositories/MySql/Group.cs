// Copyright (C) 2024-2025 ZenonEl
// This program is free  software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии

using Dapper;
using MySql.Data.MySqlClient;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlGroupGetter : IGroupGetter
{
    private readonly string _connectionString;

    public MySqlGroupGetter(string connection)
    {
        _connectionString = connection;
    }

    public async Task<IEnumerable<int>> GetGroupIDsByUserId(int userId)
    {
        const string query = @"
            SELECT ID 
            FROM UsersGroups 
            WHERE UserId = @userId";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<int>(query, new { userId });
    }

    public async Task<bool> GetGroupOwnership(int groupId, int userId)
    {
        const string query = @"
            SELECT COUNT(*) 
            FROM UsersGroups 
            WHERE ID = @groupId AND UserId = @userId";

        using var connection = new MySqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(query, new { groupId, userId });
        return count > 0;
    }

    public async Task<string> GetGroupNameById(int groupId)
    {
        const string query = @"
            SELECT GroupName 
            FROM UsersGroups 
            WHERE ID = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<string>(query, new { groupId });
    }

    public async Task<string> GetGroupDescriptionById(int groupId)
    {
        const string query = @"
            SELECT Description 
            FROM UsersGroups 
            WHERE ID = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<string>(query, new { groupId });
    }

    public async Task<int> GetGroupMemberCount(int groupId)
    {
        const string query = @"
            SELECT COUNT(*) AS MemberCount 
            FROM GroupMembers 
            WHERE GroupId = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<int>(query, new { groupId });
    }

    public async Task<bool> GetIsDefaultGroup(int groupId)
    {
        const string query = @"
            SELECT IsDefaultEnabled 
            FROM UsersGroups 
            WHERE ID = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<bool>(query, new { groupId });
    }

    public async Task<IEnumerable<int>> GetAllUsersIdsInGroup(int groupId)
    {
        const string query = @"
            SELECT ContactId 
            FROM GroupMembers 
            WHERE GroupId = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<int>(query, new { groupId });
    }

    public async Task<IEnumerable<int>> GetDefaultEnabledGroupIds(int userId)
    {
        const string query = @"
            SELECT ID
            FROM UsersGroups
            WHERE UserId = @userId AND IsDefaultEnabled = 1";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<int>(query, new { userId });
    }

    public async Task<IEnumerable<int>> GetAllUsersInGroup(int groupId, int userId)
    {
        const string query = @"
            SELECT ContactId
            FROM GroupMembers
            WHERE GroupId = @groupId AND UserId = @userId";

        using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<int>(query, new { groupId, userId });
    }


    public async Task<List<int>> GetAllUsersInDefaultEnabledGroups(int userId)
    {
        IEnumerable<int> groupIds = await GetDefaultEnabledGroupIds(userId);
        List<int> userIds = [];

        foreach (int groupId in groupIds)
        {
            userIds.AddRange(await GetAllUsersInGroup(groupId, userId));
        }

        return userIds;
    }
}

public class MySqlGroupSetter : IGroupSetter
{
    private readonly string _connectionString;

    public MySqlGroupSetter(string connection)
    {
        _connectionString = connection;
    }

    public async Task<bool> SetNewGroup(int userId, string groupName, string description)
    {
        const string query = @"
            INSERT INTO UsersGroups (UserId, GroupName, Description) 
            VALUES (@userId, @groupName, @description)";

        using var connection = new MySqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(query, new { 
            userId, 
            groupName, 
            description 
        });
        return rowsAffected > 0;
    }

    public async Task<bool> SetGroupName(int groupId, string groupName)
    {
        const string query = @"
            UPDATE UsersGroups 
            SET GroupName = @groupName 
            WHERE ID = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(query, new { 
            groupId, 
            groupName 
        });
        return rowsAffected > 0;
    }

    public async Task<bool> SetGroupDescription(int groupId, string description)
    {
        const string query = @"
            UPDATE UsersGroups 
            SET Description = @description 
            WHERE ID = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(query, new { 
            groupId, 
            description 
        });
        return rowsAffected > 0;
    }

    public async Task<bool> SetIsDefaultGroup(int groupId)
    {
        const string query = @"
            UPDATE UsersGroups 
            SET IsDefaultEnabled = NOT IsDefaultEnabled 
            WHERE ID = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(query, new { groupId });
        return rowsAffected > 0;
    }

    public async Task<bool> SetDeleteGroup(int groupId)
    {
        const string query = @"
            DELETE FROM UsersGroups 
            WHERE ID = @groupId";

        using var connection = new MySqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(query, new { groupId });
        return rowsAffected > 0;
    }
}

