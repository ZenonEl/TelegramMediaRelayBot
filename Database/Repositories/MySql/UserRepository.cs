// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Dapper;
using System.Data;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlUserRepository(IDbConnection dbConnection) : IUserRepository
{
    public bool CheckUserExists(long telegramId)
    {
        const string query = @"
            SELECT 1 
            FROM Users 
            WHERE TelegramID = @telegramId 
            LIMIT 1";

        return dbConnection.ExecuteScalar<bool>(query, new { telegramId });
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

        dbConnection.Execute(query, new { telegramID, name, link });
    }

    public void UnMuteUserByMuteId(int muteId)
    {
        string query = @$"
            UPDATE MutedContacts SET IsActive = 0 WHERE MutedId = @muteId";
        

        dbConnection.Execute(query, new { muteId });
    }

    public bool ReCreateUserSelfLink(int userId)
    {
        string newLink = Utils.GenerateUserLink();
        const string query = @"
            UPDATE Users SET Link = @newLink WHERE ID = @userId";
        

        return dbConnection.Execute(query, new { newLink, userId }) > 0;
    }
}

public class MySqlUserGetter(IDbConnection dbConnection) : IUserGetter
{
    public long GetTelegramIDbyUserID(int userId)
    {
        const string query = @"SELECT TelegramID FROM Users WHERE ID = @UserId";
        

        return dbConnection.ExecuteScalar<long>(query, new { UserId = userId });
    }

    public string GetUserNameByID(int userID)
    {
        const string query = @"SELECT Name FROM Users WHERE ID = @UserID";
        

        return dbConnection.ExecuteScalar<string?>(query, new { UserID = userID }) ?? "";
    }

    public int GetUserIDbyTelegramID(long telegramID)
    {
        const string query = @"SELECT ID FROM Users WHERE TelegramID = @TelegramID";
        

        return dbConnection.ExecuteScalar<int?>(query, new { TelegramID = telegramID }) ?? -1;
    }

    public string GetUserNameByTelegramID(long telegramID)
    {
        const string query = @"SELECT Name FROM Users WHERE TelegramID = @TelegramID";
        

        return dbConnection.ExecuteScalar<string?>(query, new { TelegramID = telegramID }) ?? string.Empty;
    }

    public List<long> GetUsersIdForMuteContactId(int contactId)
    {
        const string query = @"
            SELECT MutedByUserId 
            FROM MutedContacts 
            WHERE MutedContactId = @ContactId AND IsActive = 1";
        

        var mutedByUserIds = dbConnection.Query<int>(query, new { ContactId = contactId }).ToList();
        
        return mutedByUserIds.Select(GetTelegramIDbyUserID).ToList();
    }

    public async Task<List<long>> GetUsersIdForMuteContactIdAsync(int contactId)
    {
        const string query = @"
            SELECT MutedByUserId 
            FROM MutedContacts 
            WHERE MutedContactId = @ContactId AND IsActive = 1";


        var mutedByUserIds = (await dbConnection.QueryAsync<int>(query, new { ContactId = contactId })).ToList();
        var telegramIds = new List<long>(mutedByUserIds.Count);
        foreach (var uid in mutedByUserIds)
        {
            telegramIds.Add(GetTelegramIDbyUserID(uid));
        }
        return telegramIds;
    }

    public long GetUserTelegramIdByLink(string link)
    {
        const string query = "SELECT TelegramID FROM Users WHERE Link = @link";
        try
        {
    
            var result = dbConnection.QueryFirstOrDefault<long?>(query, new { link });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in method {MethodName}", nameof(GetUserTelegramIdByLink));
            return -1;
        }
    }

    public async Task<long> GetUserTelegramIdByLinkAsync(string link)
    {
        const string query = "SELECT TelegramID FROM Users WHERE Link = @link";
        try
        {
    
            var result = await dbConnection.QueryFirstOrDefaultAsync<long?>(query, new { link });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in method {MethodName}", nameof(GetUserTelegramIdByLinkAsync));
            return -1;
        }
    }

    private string _getUserLink(long telegramID)
    {
        const string query = "SELECT Link FROM Users WHERE TelegramID = @telegramID";
        try
        {
            var result = dbConnection.QueryFirstOrDefault<string>(query, new { telegramID });
            return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in method {MethodName}", nameof(_getUserLink));
            return string.Empty;
        }
    }

    public string GetUserSelfLink(long telegramID)
    {
        MySqlUserRepository repo = new(dbConnection);
        if (repo.CheckUserExists(telegramID))
        {
            return _getUserLink(telegramID);
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
    
            var expiredMuteIds = dbConnection.Query<int>(query).ToList();
            return expiredMuteIds;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetExpiredUsersMutes));
            return new List<int>();
        }
    }

    public async Task<List<int>> GetExpiredUsersMutesAsync()
    {
        const string query = @"
            SELECT MutedId 
            FROM MutedContacts 
            WHERE ExpirationDate <= NOW() 
            AND IsActive = 1";

        try
        {
    
            var expiredMuteIds = (await dbConnection.QueryAsync<int>(query)).ToList();
            return expiredMuteIds;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetExpiredUsersMutesAsync));
            return new List<int>();
        }
    }

    public async Task<int> GetAllUsersCount()
    {
        const string query = "SELECT EXISTS(SELECT 1 FROM Users LIMIT 1)";

        return await dbConnection.ExecuteScalarAsync<int>(query);
    }
}