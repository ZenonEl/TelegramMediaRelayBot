// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using Dapper;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

// 1. Добавляем правильный конструктор для получения IDbConnection через DI
public class MySqlOutboundDBGetter(IDbConnection dbConnection) : IOutboundDBGetter
{
    // Внутренний класс для удобного маппинга, такой же как в прошлый раз
    private class UserQueryResult
    {
        public string Name { get; init; } = string.Empty;
        public long TelegramID { get; init; }
    }

    public async Task<List<ButtonData>> GetOutboundButtonDataAsync(int userId)
    {
        // 2. ОДИН эффективный SQL-запрос с JOIN'ом
        // Отличие от Inbound в том, что здесь мы соединяем c.ContactId с u.ID
        const string query = @"
            SELECT
                u.Name,
                u.TelegramID
            FROM Contacts c
            JOIN Users u ON c.ContactId = u.ID
            WHERE c.UserId = @userId AND c.status = 'waiting_for_accept'";

        // 3. Используем Dapper для безопасного выполнения запроса
        IEnumerable<UserQueryResult> users = await dbConnection.QueryAsync<UserQueryResult>(query, new { userId });

        // 4. Преобразуем результат в нужный формат, как и раньше
        List<ButtonData> buttonDataList = users
            .Select(user => new ButtonData
            {
                ButtonText = user.Name,
                // Используем правильный callback_data из оригинального кода
                CallbackData = "user_show_outbound_invite:" + user.TelegramID
            })
            .ToList();

        return buttonDataList;
    }

    // 5. Старые приватные методы GetContactUserIdsAsync и GetUserDataByUserIdAsync
    //    больше не нужны и удаляются.
}
