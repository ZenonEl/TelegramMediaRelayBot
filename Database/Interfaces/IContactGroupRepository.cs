// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IContactGroupRepository
{
    bool AddContactToGroup(int userId, int contactId, int groupId);
    bool RemoveContactFromGroup(int userId, int contactId, int groupId);
    bool CheckUserAndContactConnect(int userId, int contactId);
}
