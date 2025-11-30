// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IContactRepository
{
    Task<int> AddContactAsync(int userId, int contactId, string status);
    Task<int> UpsertMutedContactAsync(int mutedByUserId, int mutedContactId, DateTime? expirationDate);
    Task<int> UnMuteUserByMuteId(int muteId);
    Task<int> DeactivateMutedContactAsync(int userId, int contactId);
    Task<int> RemoveContactByStatusAsync(int senderId, int accepterId, string? status);
    Task<int> RemoveContactsBatchAsync(int userId, List<int> contactIds);
    Task<int> RemoveGroupMembersBatchAsync(int userId, List<int> contactIds);
    Task<int> RemoveAllContactsExceptAsync(int userId, List<int> excludeIds);
    Task<int> RemoveAllGroupMembersExceptAsync(int userId, List<int> excludeIds);
    Task<int> RemoveAllContactsAsync(int userId);
    Task<int> RemoveAllGroupMembersAsync(int userId);
    Task<int> UpdateContactStatusAsync(int userId, int contactId, string status);
}