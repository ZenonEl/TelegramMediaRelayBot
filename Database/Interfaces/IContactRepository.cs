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
    string GetActiveMuteTimeByContactID(int contactID);
    int GetContactIDByLink(string link);
    int GetContactByTelegramID(long telegramID);

    // Async counterparts for hot paths
    Task<string> GetActiveMuteTimeByContactIDAsync(int contactID);
    Task<int> GetContactIDByLinkAsync(string link);
    Task<int> GetContactByTelegramIDAsync(long telegramID);
}
