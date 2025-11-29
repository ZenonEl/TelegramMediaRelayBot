// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.Database.Repositories.MySql;


using Dapper;


// Конструктор остается прежним, он уже правильный
public class MySqlInboundDBGetter(IDbConnection dbConnection) : IInboundDBGetter
{
    // Внутренний класс для удобного маппинга результата из Dapper
    private class UserQueryResult
    {
        public string Name { get; init; } = string.Empty;
        public long TelegramID { get; init; }
    }

    public async Task<List<ButtonData>> GetInboundsButtonDataAsync(int userId)
    {
        // 1. ОДИН эффективный запрос вместо N+1
        // Мы объединяем таблицы Contacts и Users, чтобы получить все данные сразу.
        const string query = @"
            SELECT
                u.Name,
                u.TelegramID
            FROM Contacts c
            JOIN Users u ON c.UserId = u.ID
            WHERE c.ContactId = @userId AND c.status = 'waiting_for_accept'";

        // 2. Используем Dapper для выполнения запроса.
        // Он безопасен и сам управляет открытием/закрытием подключения.
        // Мы БОЛЬШЕ НИКОГДА не пишем 'using (dbConnection)' для внедренного подключения.
        var users = await dbConnection.QueryAsync<UserQueryResult>(query, new { userId });

        // 3. Преобразуем (мапим) результат в нужный нам формат ButtonData
        var buttonDataList = users
            .Select(user => new ButtonData
            {
                ButtonText = user.Name,
                CallbackData = "user_show_inbounds_invite:" + user.TelegramID
            })
            .ToList();

        return buttonDataList;
    }
}