// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteContactAdder(IContactUoW contactUoWService) : IContactAdder
{
    public Task AddContact(long telegramId, string link)
    {
        return contactUoWService.AddContactAsync(telegramId, link, ContactsStatus.WAITING_FOR_ACCEPT);
    }

    public Task<bool> AddMutedContact(int mutedByUserId, int mutedContactId, DateTime? expirationDate = null, DateTime muteDate = default)
    {
        return contactUoWService.MuteContactAsync(mutedByUserId, mutedContactId, expirationDate)
            .ContinueWith(t => t.IsCompletedSuccessfully);
    }
}

public class SqliteContactRemover(IContactUoW contactUoWService) : IContactRemover
{
    public Task RemoveMutedContact(int userId, int contactId)
    {
        return contactUoWService.UnmuteContactAsync(userId, contactId);
    }

    public Task<bool> RemoveContactByStatus(int senderTelegramId, int accepterTelegramId, string? status = null)
    {
        return contactUoWService.RemoveContactByStatusAsync(senderTelegramId, accepterTelegramId, status)
            .ContinueWith(t => t.IsCompletedSuccessfully);
    }

    public Task<bool> RemoveUsersFromContacts(int userId, List<int> contactIds)
    {
        return contactUoWService.RemoveUsersFromContactsAsync(userId, contactIds)
            .ContinueWith(t => t.IsCompletedSuccessfully);
    }

    public Task<bool> RemoveAllContactsExcept(int userId, List<int> excludeIds)
    {
        return contactUoWService.RemoveAllUserContactsAsync(userId, excludeIds)
            .ContinueWith(t => t.IsCompletedSuccessfully);
    }

    public Task<bool> RemoveAllContacts(int userId)
    {
        return contactUoWService.RemoveAllUserContactsAsync(userId, null)
            .ContinueWith(t => t.IsCompletedSuccessfully);
    }
}

// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------

public class SqliteContactSetter(IContactUoW contactUoWService) : IContactSetter
{
    public Task SetContactStatus(long senderTelegramId, long accepterTelegramId, string status)
    {
        return contactUoWService.UpdateContactStatusAsync(senderTelegramId, accepterTelegramId, status);
    }
}

public class SqliteContactGetter(IDbConnection dbConnection, TelegramMediaRelayBot.Config.Services.IResourceService resourceService) : IContactGetter
{

    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService = resourceService;

    public async Task<List<long>> GetAllContactUserTGIds(int userId)
    {
        try
        {

            SqliteUserGetter userGetter = new(dbConnection);

            IEnumerable<(long UserId, long ContactId)> results = await dbConnection.QueryAsync<(long UserId, long ContactId)>(
                @"SELECT UserId, ContactId
                FROM Contacts
                WHERE (ContactId = @UserId OR UserId = @UserId)
                AND status = @Status",
                new { UserId = userId, Status = ContactsStatus.ACCEPTED });

            List<long> contactUserIds = results
                .SelectMany(row => new[] { row.UserId, row.ContactId })
                .Where(id => id != userId)
                .Distinct()
                .ToList();

            return contactUserIds
                .Select(contactUserId => userGetter.GetTelegramIDbyUserID((int)contactUserId))
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving contact user IDs");
            return new List<long>();
        }
    }

    public async Task<List<int>> GetAllContactUserIds(int userId)
    {
        try
        {
            IEnumerable<long> results = await dbConnection.QueryAsync<long>(
                @"SELECT DISTINCT CASE
                    WHEN UserId = @UserId THEN ContactId
                    ELSE UserId
                END AS ContactId
                FROM Contacts
                WHERE (UserId = @UserId OR ContactId = @UserId)
                AND Status = @Status",
                new { UserId = userId, Status = ContactsStatus.ACCEPTED });

            return results.Select(id => (int)id).Where(id => id != userId).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving contact user IDs");
            return new List<int>();
        }
    }

    public async Task<IEnumerable<int>> GetMutedContactIds(int userId)
    {
        const string query = @"
                SELECT MutedContactId
                FROM MutedContacts
                WHERE MutedByUserId = @userId
                AND (ExpirationDate IS NULL OR ExpirationDate > datetime('now'))";

        return (await dbConnection.QueryAsync<int>(query, new { userId })).ToList();
    }

    public string GetActiveMuteTimeByContactID(int contactID)
    {
        try
        {


            DateTime? expirationDate = dbConnection.QueryFirstOrDefault<DateTime?>(
                @"SELECT ExpirationDate
                FROM MutedContacts
                WHERE MutedContactId = @contactID
                AND IsActive = 1",
                new { contactID });

            return expirationDate?.ToString("yyyy-MM-dd HH:mm:ss")
                ?? _resourceService.GetResourceString("NoActiveMute");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetActiveMuteTimeByContactID));
            return "";
        }
    }

    public async Task<string> GetActiveMuteTimeByContactIDAsync(int contactID)
    {
        try
        {

            DateTime? expirationDate = await dbConnection.QueryFirstOrDefaultAsync<DateTime?>(
                @"SELECT ExpirationDate
                FROM MutedContacts
                WHERE MutedContactId = @contactID
                AND IsActive = 1",
                new { contactID });

            return expirationDate?.ToString("yyyy-MM-dd HH:mm:ss")
                ?? _resourceService.GetResourceString("NoActiveMute");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetActiveMuteTimeByContactIDAsync));
            return "";
        }
    }

    public int GetContactIDByLink(string link)
    {
        const string query = "SELECT ID FROM Users WHERE Link = @link";
        try
        {

            int? result = dbConnection.QueryFirstOrDefault<int?>(query, new { link });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetContactIDByLink));
            return -1;
        }
    }

    public async Task<int> GetContactIDByLinkAsync(string link)
    {
        const string query = "SELECT ID FROM Users WHERE Link = @link";
        try
        {

            int? result = await dbConnection.QueryFirstOrDefaultAsync<int?>(query, new { link });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetContactIDByLinkAsync));
            return -1;
        }
    }

    public int GetContactByTelegramID(long telegramID)
    {
        const string query = "SELECT ID FROM Users WHERE TelegramID = @telegramID";
        try
        {

            int? result = dbConnection.QueryFirstOrDefault<int?>(query, new { telegramID });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetContactByTelegramID));
            return -1;
        }
    }

    public async Task<int> GetContactByTelegramIDAsync(long telegramID)
    {
        const string query = "SELECT ID FROM Users WHERE TelegramID = @telegramID";
        try
        {

            int? result = await dbConnection.QueryFirstOrDefaultAsync<int?>(query, new { telegramID });
            return result ?? -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method {MethodName}", nameof(GetContactByTelegramIDAsync));
            return -1;
        }
    }
}
