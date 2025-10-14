using System.Collections.Concurrent;

namespace TelegramBot.Services;

/// <summary>
/// Хранит последнее текстовое сообщение от каждого пользователя для "сцепки" с медиа-ссылкой.
/// Регистрируется как Singleton.
/// </summary>
public interface ILastUserTextCache
{
    void Set(long chatId, string text);
    (bool Found, string Text, DateTime At) TryGet(long chatId);
}

public class LastUserTextCache : ILastUserTextCache
{
    private readonly ConcurrentDictionary<long, (string Text, DateTime At)> _lastTextByChatId = new();

    public void Set(long chatId, string text)
    {
        _lastTextByChatId[chatId] = (text, DateTime.UtcNow);
    }

    public (bool Found, string Text, DateTime At) TryGet(long chatId)
    {
        if (_lastTextByChatId.TryGetValue(chatId, out var v))
        {
            return (true, v.Text, v.At);
        }
        return (false, string.Empty, DateTime.MinValue);
    }
}