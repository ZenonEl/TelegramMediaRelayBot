// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace TelegramBot.Services;

/// <summary>
/// Управляет очередностью обработки запросов от одного пользователя, чтобы избежать спама.
/// Регистрируется как Singleton.
/// </summary>
public interface IUserRequestThrottler
{
    TimeSpan ReserveSlot(long chatId, TimeSpan baseDelay);
}

public class UserRequestThrottler : IUserRequestThrottler
{
    private readonly ConcurrentDictionary<long, DateTime> _nextSlotByChatId = new();

    public TimeSpan ReserveSlot(long chatId, TimeSpan baseDelay)
    {
        DateTime now = DateTime.UtcNow;
        // Потокобезопасно получаем или добавляем значение
        DateTime next = _nextSlotByChatId.GetOrAdd(chatId, now);

        TimeSpan wait = (next > now ? next - now : TimeSpan.Zero) + baseDelay;
        DateTime newNextSlot = now + wait;

        // Потокобезопасно обновляем значение
        _nextSlotByChatId.AddOrUpdate(chatId, newNextSlot, (key, oldVal) => newNextSlot > oldVal ? newNextSlot : oldVal);

        return wait;
    }
}
