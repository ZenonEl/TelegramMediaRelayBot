// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IUserTimeService
{
    /// <summary>
    /// Преобразует UTC время в локальное время пользователя.
    /// Сейчас возвращает UTC, в будущем будет добавлять смещение из настроек.
    /// </summary>
    DateTime ConvertToUserTime(int userId, DateTime utcDateTime);

    /// <summary>
    /// Возвращает красиво отформатированную строку времени для пользователя.
    /// </summary>
    string FormatTimeForUser(int userId, DateTime? utcDateTime);
}

public class UserTimeService : IUserTimeService
{
    // Сюда в будущем внедрим IUserRepository, чтобы брать TimezoneOffset

    public DateTime ConvertToUserTime(int userId, DateTime utcDateTime)
    {
        // TODO: В будущем получать offset пользователя из БД.
        // Пока возвращаем UTC (или можно DateTime.Now если сервер в нужном поясе, но лучше UTC)
        return utcDateTime;
    }

    public string FormatTimeForUser(int userId, DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue) return "навсегда"; // TODO Move: "Time.Forever"

        DateTime userTime = ConvertToUserTime(userId, utcDateTime.Value);

        // Формат: "30.11.2025 15:30 (UTC)"
        // В будущем уберем (UTC) и будем писать локальное время
        return $"{userTime:dd.MM.yyyy HH:mm} UTC";
    }
}
