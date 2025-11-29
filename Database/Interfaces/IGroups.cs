// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IGroupGetter
{
    Task<IEnumerable<int>> GetGroupIDsByUserId(int userId);
    Task<bool> GetGroupOwnership(int groupId, int userId);
    Task<string> GetGroupNameById(int groupId);
    Task<string> GetGroupDescriptionById(int groupId);
    Task<int> GetGroupMemberCount(int groupId);
    Task<bool> GetIsDefaultGroup(int groupId);
    Task<IEnumerable<int>> GetAllUsersIdsInGroup(int groupId);
    Task<IEnumerable<int>> GetDefaultEnabledGroupIds(int userId);
    Task<List<int>> GetAllUsersInDefaultEnabledGroups(int userId);
    Task<IEnumerable<int>> GetAllUsersInGroup(int groupId, int userId);
}

public interface IGroupSetter
{
    Task<bool> SetNewGroup(int userId, string groupName, string description);
    Task<bool> SetGroupName(int groupId, string groupName);
    Task<bool> SetGroupDescription(int groupId, string description);
    Task<bool> SetIsDefaultGroup(int groupId);
    Task<bool> SetDeleteGroup(int groupId);
}


