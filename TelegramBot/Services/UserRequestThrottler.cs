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
        var now = DateTime.UtcNow;
        // Потокобезопасно получаем или добавляем значение
        var next = _nextSlotByChatId.GetOrAdd(chatId, now);
        
        var wait = (next > now ? next - now : TimeSpan.Zero) + baseDelay;
        var newNextSlot = now + wait;

        // Потокобезопасно обновляем значение
        _nextSlotByChatId.AddOrUpdate(chatId, newNextSlot, (key, oldVal) => newNextSlot > oldVal ? newNextSlot : oldVal);
        
        return wait;
    }
}