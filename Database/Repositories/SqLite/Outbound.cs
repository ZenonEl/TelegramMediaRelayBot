// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

// Конструктор уже правильный, мы его не трогаем.
public class SqliteOutboundDBGetter(IDbConnection dbConnection) : IOutboundDBGetter
{
    // Внутренний класс для удобного маппинга, как и в MySql-версии.
    private class UserQueryResult
    {
        public string Name { get; init; } = string.Empty;
        public long TelegramID { get; init; }
    }

    public async Task<List<ButtonData>> GetOutboundButtonDataAsync(int userId)
    {
        // 1. Тот же самый эффективный SQL-запрос с JOIN.
        // Он полностью совместим со SQLite.
        const string query = @"
            SELECT
                u.Name,
                u.TelegramID
            FROM Contacts c
            JOIN Users u ON c.ContactId = u.ID
            WHERE c.UserId = @userId AND c.status = 'waiting_for_accept'";

        // 2. Используем Dapper. Он отлично работает со SQLite.
        // Вся работа с подключением, командами и чтением инкапсулирована здесь.
        var users = await dbConnection.QueryAsync<UserQueryResult>(query, new { userId });

        // 3. Преобразуем результат в итоговую модель данных.
        var buttonDataList = users
            .Select(user => new ButtonData
            {
                ButtonText = user.Name,
                CallbackData = "user_show_outbound_invite:" + user.TelegramID
            })
            .ToList();

        return buttonDataList;
    }

    // 4. Старые, небезопасные и неэффективные приватные методы
    //    GetContactUserIdsAsync и GetUserDataByUserIdAsync удаляются.
}
