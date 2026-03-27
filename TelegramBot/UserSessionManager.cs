// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using System.Collections.Concurrent;


namespace TelegramMediaRelayBot;

public static class UserSessionManager
{
    private static readonly ConcurrentDictionary<long, IUserState> _states = new();
    private static readonly ConcurrentDictionary<long, DateTime> _createdAt = new();
    private static Timer? _cleanupTimer;

    public static void StartCleanupTimer()
    {
        _cleanupTimer = new Timer(
            _ => CleanupExpiredSessions(),
            null,
            TimeSpan.FromMinutes(Config.sessionCleanupIntervalMinutes),
            TimeSpan.FromMinutes(Config.sessionCleanupIntervalMinutes)
        );
        Log.Information("Session cleanup timer started (interval: {Interval} min, TTL: {TTL} min)",
            Config.sessionCleanupIntervalMinutes, Config.sessionTtlMinutes);
    }

    public static IUserState? Get(long chatId)
    {
        _states.TryGetValue(chatId, out var state);
        return state;
    }

    public static bool TryGetValue(long chatId, out IUserState? state)
    {
        return _states.TryGetValue(chatId, out state);
    }

    public static bool ContainsKey(long chatId)
    {
        return _states.ContainsKey(chatId);
    }

    public static void Set(long chatId, IUserState state)
    {
        _states[chatId] = state;
        _createdAt[chatId] = DateTime.UtcNow;
    }

    public static bool Remove(long chatId)
    {
        _createdAt.TryRemove(chatId, out _);
        return _states.TryRemove(chatId, out _);
    }

    public static bool Remove(long chatId, out IUserState? state)
    {
        _createdAt.TryRemove(chatId, out _);
        return _states.TryRemove(chatId, out state);
    }

    private static void CleanupExpiredSessions()
    {
        var now = DateTime.UtcNow;
        var ttl = TimeSpan.FromMinutes(Config.sessionTtlMinutes);
        int cleaned = 0;

        foreach (var kvp in _createdAt)
        {
            if (now - kvp.Value <= ttl) continue;

            long chatId = kvp.Key;
            if (_states.TryRemove(chatId, out var state))
            {
                if (state is ProcessVideoDC videoDc)
                {
                    try { videoDc.timeoutCTS.Cancel(); }
                    catch (ObjectDisposedException) { }
                }

                _createdAt.TryRemove(chatId, out _);
                cleaned++;
                Log.Debug("Expired session removed for chat {ChatId}", chatId);
            }
        }

        if (cleaned > 0)
        {
            Log.Information("Session cleanup: removed {Count} expired session(s), {Remaining} active",
                cleaned, _states.Count);
        }
    }
}
