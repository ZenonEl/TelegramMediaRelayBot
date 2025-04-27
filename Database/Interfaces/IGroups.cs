// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

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


