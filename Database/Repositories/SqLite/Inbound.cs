// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

// Конструктор уже принимает IDbConnection, но был пуст.
// Теперь мы используем первичный конструктор, который автоматически делает dbConnection доступным.
public class SqliteInboundDBGetter(IDbConnection dbConnection) : IInboundDBGetter
{
    // Внутренний класс для удобного маппинга, как и в предыдущих случаях.
    private class UserQueryResult
    {
        public string Name { get; init; } = string.Empty;
        public long TelegramID { get; init; }
    }

    public async Task<List<ButtonData>> GetInboundsButtonDataAsync(int userId)
    {
        // 1. Единый, эффективный и совместимый со SQLite SQL-запрос.
        // Отличие от Outbound в том, что здесь мы соединяем c.UserId с u.ID.
        const string query = @"
            SELECT
                u.Name,
                u.TelegramID
            FROM Contacts c
            JOIN Users u ON c.UserId = u.ID
            WHERE c.ContactId = @userId AND c.status = 'waiting_for_accept'";

        // 2. Используем Dapper для безопасного и чистого выполнения запроса.
        IEnumerable<UserQueryResult> users = await dbConnection.QueryAsync<UserQueryResult>(query, new { userId });

        // 3. Преобразуем полученные данные в итоговую модель.
        List<ButtonData> buttonDataList = users
            .Select(user => new ButtonData
            {
                ButtonText = user.Name,
                // Используем правильный callback_data из оригинального кода
                CallbackData = "user_show_inbounds_invite:" + user.TelegramID
            })
            .ToList();

        return buttonDataList;
    }
}
