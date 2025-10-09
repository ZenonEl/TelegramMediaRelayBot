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
using System.Data;
using MySql.Data.MySqlClient;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlGroupGetter(IDbConnection dbConnection) : IGroupGetter
{
    public async Task<IEnumerable<int>> GetGroupIDsByUserId(int userId)
    {
        const string query = @"
            SELECT ID 
            FROM UsersGroups 
            WHERE UserId = @userId";


        return await dbConnection.QueryAsync<int>(query, new { userId });
    }

    public async Task<bool> GetGroupOwnership(int groupId, int userId)
    {
        const string query = @"
            SELECT COUNT(*) 
            FROM UsersGroups 
            WHERE ID = @groupId AND UserId = @userId";


        var count = await dbConnection.ExecuteScalarAsync<int>(query, new { groupId, userId });
        return count > 0;
    }

    public async Task<string> GetGroupNameById(int groupId)
    {
        const string query = @"
            SELECT GroupName 
            FROM UsersGroups 
            WHERE ID = @groupId";


        return await dbConnection.QueryFirstOrDefaultAsync<string>(query, new { groupId });
    }

    public async Task<string> GetGroupDescriptionById(int groupId)
    {
        const string query = @"
            SELECT Description 
            FROM UsersGroups 
            WHERE ID = @groupId";


        return await dbConnection.QueryFirstOrDefaultAsync<string>(query, new { groupId });
    }

    public async Task<int> GetGroupMemberCount(int groupId)
    {
        const string query = @"
            SELECT COUNT(*) AS MemberCount 
            FROM GroupMembers 
            WHERE GroupId = @groupId";


        return await dbConnection.QueryFirstOrDefaultAsync<int>(query, new { groupId });
    }

    public async Task<bool> GetIsDefaultGroup(int groupId)
    {
        const string query = @"
            SELECT IsDefaultEnabled 
            FROM UsersGroups 
            WHERE ID = @groupId";


        return await dbConnection.QueryFirstOrDefaultAsync<bool>(query, new { groupId });
    }

    public async Task<IEnumerable<int>> GetAllUsersIdsInGroup(int groupId)
    {
        const string query = @"
            SELECT ContactId 
            FROM GroupMembers 
            WHERE GroupId = @groupId";


        return await dbConnection.QueryAsync<int>(query, new { groupId });
    }

    public async Task<IEnumerable<int>> GetDefaultEnabledGroupIds(int userId)
    {
        const string query = @"
            SELECT ID
            FROM UsersGroups
            WHERE UserId = @userId AND IsDefaultEnabled = 1";


        return await dbConnection.QueryAsync<int>(query, new { userId });
    }

    public async Task<IEnumerable<int>> GetAllUsersInGroup(int groupId, int userId)
    {
        const string query = @"
            SELECT ContactId
            FROM GroupMembers
            WHERE GroupId = @groupId AND UserId = @userId";


        return await dbConnection.QueryAsync<int>(query, new { groupId, userId });
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

public class MySqlGroupSetter(IGroupSetter uowService) : IGroupSetter
{
    public Task<bool> SetNewGroup(int userId, string groupName, string description)
    {
        return uowService.SetNewGroup(userId, groupName, description);
    }

    public Task<bool> SetGroupName(int groupId, string groupName)
    {
        return uowService.SetGroupName(groupId, groupName);
    }

    public Task<bool> SetGroupDescription(int groupId, string description)
    {
        return uowService.SetGroupDescription(groupId, description);
    }

    public Task<bool> SetIsDefaultGroup(int groupId)
    {
        return uowService.SetIsDefaultGroup(groupId);
    }

    public Task<bool> SetDeleteGroup(int groupId)
    {
        return uowService.SetDeleteGroup(groupId);
    }
}