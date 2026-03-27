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
    void AddContact(long telegramID, string link);
    bool MuteContact(int userId, int contactId, DateTime? mutedUntil = null);
}

public interface IContactRemover
{
    bool UnmuteContact(int userId, int contactId);
    bool RemoveContactByStatus(int senderTelegramID, int accepterTelegramID, string? status = null);
    bool RemoveUsersFromContacts(int userId, List<int> contactIds);
    bool RemoveAllContactsExcept(int userId, List<int> excludeIds);
    bool RemoveAllContacts(int userId);
}

public interface IContactSetter
{
    void SetContactStatus(long SenderTelegramID, long AccepterTelegramID, string status);
}

public interface IContactGetter
{
    Task<List<long>> GetAllContactUserTGIds(int userId);
    Task<List<int>> GetAllContactUserIds(int userId);
    DateTime? GetMutedUntil(int userId, int contactId);
    int GetContactIDByLink(string link);
    int GetContactByTelegramID(long telegramID);
}
