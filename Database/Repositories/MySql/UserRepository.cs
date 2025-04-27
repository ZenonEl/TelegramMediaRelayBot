// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Dapper;
using MySql.Data.MySqlClient;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlUserRepository(string connectionString) : IUserRepository
{
    private readonly string _connectionString = connectionString;

    public bool CheckUserExists(long telegramId)
    {
        const string query = @"
            SELECT 1 
            FROM Users 
            WHERE TelegramID = @telegramId 
            LIMIT 1";

        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<bool>(query, new { telegramId });
    }

    public void AddUser(string name, long telegramID, bool user)
    {
        string link = Utils.GenerateUserLink();

        if (user)
        {
            return;
        }

        const string query = @$"
            INSERT INTO Users (TelegramID, Name, Link) VALUES (@telegramID, @name, @link)";

        using var connection = new MySqlConnection(_connectionString);
        connection.Execute(query, new { telegramID, name, link });
    }

    public void UnMuteUserByMuteId(int muteId)
    {
        string query = @$"
            UPDATE MutedContacts SET IsActive = 0 WHERE MutedId = @muteId";
        
        using var connection = new MySqlConnection(_connectionString);
        connection.Execute(query, new { muteId });
    }

    public bool ReCreateUserSelfLink(int userId)
    {
        string newLink = Utils.GenerateUserLink();
        const string query = @"
            UPDATE Users SET Link = @newLink WHERE ID = @userId";
        
        using var connection = new MySqlConnection(_connectionString);
        return connection.Execute(query, new { newLink, userId }) > 0;
    }
}

public class MySqlUserGetter(string connectionString) : IUserGetter
{
    private readonly string _connectionString = connectionString;

    public long GetTelegramIDbyUserID(int userId)
    {
        const string query = @"SELECT TelegramID FROM Users WHERE ID = @UserId";
        
        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<long>(query, new { UserId = userId });
    }

    public string GetUserNameByID(int userID)
    {
        const string query = @"SELECT Name FROM Users WHERE ID = @UserID";
        
        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<string?>(query, new { UserID = userID }) ?? "";
    }

    public int GetUserIDbyTelegramID(long telegramID)
    {
        const string query = @"SELECT ID FROM Users WHERE TelegramID = @TelegramID";
        
        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<int?>(query, new { TelegramID = telegramID }) ?? -1;
    }

    public string GetUserNameByTelegramID(long telegramID)
    {
        const string query = @"SELECT Name FROM Users WHERE TelegramID = @TelegramID";
        
        using var connection = new MySqlConnection(_connectionString);
        return connection.ExecuteScalar<string?>(query, new { TelegramID = telegramID }) ?? string.Empty;
    }

    public List<long> GetUsersIdForMuteContactId(int contactId)
    {
        const string query = @"
            SELECT MutedByUserId 
            FROM MutedContacts 
            WHERE MutedContactId = @ContactId AND IsActive = 1";
        
        using var connection = new MySqlConnection(_connectionString);
        var mutedByUserIds = connection.Query<int>(query, new { ContactId = contactId }).ToList();
        
        return mutedByUserIds.Select(GetTelegramIDbyUserID).ToList();
    }

    public long GetUserTelegramIdByLink(string link)
    {
        const string query = "SELECT TelegramID FROM Users WHERE Link = @link";
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            var result = connection.QueryFirstOrDefault<long?>(query, new { link });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in method {MethodName}", nameof(GetUserTelegramIdByLink));
            return -1;
        }
    }

    private static string GetUserLink(long telegramID, string connectionString)
    {
        const string query = "SELECT Link FROM Users WHERE TelegramID = @telegramID";
        try
        {
            using var connection = new MySqlConnection(connectionString);
            var result = connection.QueryFirstOrDefault<string>(query, new { telegramID });
            return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in method {MethodName}", nameof(GetUserLink));
            return string.Empty;
        }
    }

    public string GetUserSelfLink(long telegramID)
    {
        MySqlUserRepository repo = new(_connectionString);
        if (repo.CheckUserExists(telegramID))
        {
            return GetUserLink(telegramID, _connectionString);
        }
        return "";
    }

    public List<int> GetExpiredUsersMutes()
    {
        const string query = @"
            SELECT MutedId 
            FROM MutedContacts 
            WHERE ExpirationDate <= NOW() 
            AND IsActive = 1";

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            var expiredMuteIds = connection.Query<int>(query).ToList();
            return expiredMuteIds;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetExpiredUsersMutes));
            return new List<int>();
        }
    }

    public async Task<int> GetAllUsersCount()
    {
        const string query = "SELECT EXISTS(SELECT 1 FROM Users LIMIT 1)";
        using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(query);
    }
}