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

public interface IUserRepository
{
    bool CheckUserExists(long telegramId);
    void AddUser(string name, long telegramID, bool user);
    void UnMuteUserByMuteId(int userId);
    bool ReCreateUserSelfLink(int userId);
}

public interface IUserGetter
{
    long GetTelegramIDbyUserID(int userID);
    string? GetUserNameByID(int userID);
    int GetUserIDbyTelegramID(long telegramID);
    string GetUserNameByTelegramID(long telegramID);
    List<long> GetUsersIdForMuteContactId(int contactId);
    List<int> GetExpiredUsersMutes();
    long GetUserTelegramIdByLink(string link);
    string GetUserSelfLink(long telegramId);
    Task<int> GetAllUsersCount();
}

