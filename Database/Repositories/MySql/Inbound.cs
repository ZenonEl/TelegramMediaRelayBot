// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

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