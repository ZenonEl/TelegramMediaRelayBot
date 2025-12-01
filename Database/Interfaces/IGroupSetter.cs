// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IGroupUoW
{
    Task<bool> SetNewGroup(int userId, string groupName, string description);
    Task<bool> SetGroupName(int groupId, string groupName);
    Task<bool> SetGroupDescription(int groupId, string description);
    Task<bool> SetIsDefaultGroup(int groupId);
    Task<bool> SetDeleteGroup(int groupId);
}
