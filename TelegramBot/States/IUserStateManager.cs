// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.States; // Добавь этот using

namespace TelegramMediaRelayBot;

// Интерфейс теперь работает с UserStateData
public interface IUserStateManager
{
    bool TryGet(long chatId, out UserStateData? state);
    void Set(long chatId, UserStateData state);
    bool Remove(long chatId);
    bool Contains(long chatId);
}

// Реализация тоже меняется на UserStateData
public class InMemoryUserStateManager : IUserStateManager
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, UserStateData> _states = new();

    public bool TryGet(long chatId, out UserStateData? state)
    {
        if (_states.TryGetValue(chatId, out UserStateData? s))
        {
            state = s;
            return true;
        }
        state = null;
        return false;
    }

    public void Set(long chatId, UserStateData state)
    {
        _states[chatId] = state;
    }

    public bool Remove(long chatId)
    {
        return _states.TryRemove(chatId, out _);
    }

    public bool Contains(long chatId)
    {
        return _states.ContainsKey(chatId);
    }
}
