// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IContactUoW
{
    Task AddContactAsync(long userTelegramId, string contactLink, string status);
    Task MuteContactAsync(int mutedByUserId, int mutedContactId, DateTime? expirationDate);
    Task UnmuteContactAsync(int userId, int contactId);
    Task UnMuteUserByMuteId(int muteId);
    Task RemoveContactByStatusAsync(int senderTelegramId, int accepterTelegramId, string? status = null);
    Task RemoveUsersFromContactsAsync(int userId, List<int> contactIds);
    Task RemoveAllUserContactsAsync(int userId, List<int>? excludeIds = null);
    Task UpdateContactStatusAsync(long senderTelegramId, long accepterTelegramId, string status);
}