// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

namespace TelegramMediaRelayBot;

public interface IUserStateManager
{
    bool TryGet(long chatId, out IUserState? state);
    void Set(long chatId, IUserState state);
    bool Remove(long chatId);
    bool Contains(long chatId);
}

public class InMemoryUserStateManager : IUserStateManager
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, IUserState> _states = new();

    public bool TryGet(long chatId, out IUserState? state)
    {
        if (_states.TryGetValue(chatId, out var s))
        {
            state = s;
            return true;
        }
        state = null;
        return false;
    }

    public void Set(long chatId, IUserState state)
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