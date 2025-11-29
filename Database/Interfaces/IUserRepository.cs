// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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

    // Async versions for high-traffic paths
    Task<List<int>> GetExpiredUsersMutesAsync();
    Task<List<long>> GetUsersIdForMuteContactIdAsync(int contactId);
    Task<long> GetUserTelegramIdByLinkAsync(string link);
}

