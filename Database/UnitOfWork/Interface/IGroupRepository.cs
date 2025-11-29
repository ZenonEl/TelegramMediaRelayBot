// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IGroupRepository
{
    Task<int> CreateGroup(int userId, string groupName, string description);
    Task<int> UpdateGroupName(int groupId, string groupName);
    Task<int> UpdateGroupDescription(int groupId, string description);
    Task<int> ToggleDefaultStatus(int groupId);
    Task<int> DeleteGroup(int groupId);
}