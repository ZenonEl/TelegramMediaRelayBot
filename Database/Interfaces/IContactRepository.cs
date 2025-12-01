// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IContactAdder
{
    Task AddContact(long telegramID, string link);
    Task<bool> AddMutedContact(int mutedByUserId, int mutedContactId, DateTime? expirationDate = null, DateTime muteDate = default);
}

public interface IContactRemover
{
    Task RemoveMutedContact(int userId, int contactId);
    Task<bool> RemoveContactByStatus(int senderTelegramID, int accepterTelegramID, string? status = null);
    Task<bool> RemoveUsersFromContacts(int userId, List<int> contactIds);
    Task<bool> RemoveAllContactsExcept(int userId, List<int> excludeIds);
    Task<bool> RemoveAllContacts(int userId);
}

public interface IContactSetter
{
    Task SetContactStatus(long SenderTelegramID, long AccepterTelegramID, string status);
}

public interface IContactGetter
{
    Task<List<long>> GetAllContactUserTGIds(int userId);
    Task<List<int>> GetAllContactUserIds(int userId);
    Task<IEnumerable<int>> GetMutedContactIds(int userId);
    string GetActiveMuteTimeByContactID(int contactID);
    int GetContactIDByLink(string link);
    int GetContactByTelegramID(long telegramID);

    // Async counterparts for hot paths
    Task<string> GetActiveMuteTimeByContactIDAsync(int contactID);
    Task<int> GetContactIDByLinkAsync(string link);
    Task<int> GetContactByTelegramIDAsync(long telegramID);
}
