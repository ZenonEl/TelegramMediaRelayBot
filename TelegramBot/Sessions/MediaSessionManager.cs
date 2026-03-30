// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.


using System.Collections.Concurrent;


namespace TelegramMediaRelayBot.TelegramBot.Sessions;

public static class MediaSessionManager
{
    private static readonly ConcurrentDictionary<string, MediaSession> _sessions = new();
    private static Timer? _cleanupTimer;

    public static void StartCleanupTimer()
    {
        _cleanupTimer = new Timer(
            _ => CleanupExpired(),
            null,
            TimeSpan.FromMinutes(Config.sessionCleanupIntervalMinutes),
            TimeSpan.FromMinutes(Config.sessionCleanupIntervalMinutes)
        );
        Log.Information("MediaSession cleanup timer started (interval: {Interval} min, TTL: {TTL} min)",
            Config.sessionCleanupIntervalMinutes, Config.sessionTtlMinutes);
    }

    public static MediaSession Create(string sessionId, long chatId, string url, string? caption = null)
    {
        var session = new MediaSession(sessionId, chatId, url, caption);
        _sessions[sessionId] = session;
        return session;
    }

    public static MediaSession? Get(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public static bool TryGet(string sessionId, out MediaSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public static bool Remove(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            try { session.Cts.Cancel(); }
            catch (ObjectDisposedException) { }
            return true;
        }
        return false;
    }

    public static bool Remove(string sessionId, out MediaSession? session)
    {
        if (_sessions.TryRemove(sessionId, out session))
        {
            try { session.Cts.Cancel(); }
            catch (ObjectDisposedException) { }
            return true;
        }
        session = null;
        return false;
    }

    private static void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        var ttl = TimeSpan.FromMinutes(Config.sessionTtlMinutes);
        int cleaned = 0;

        foreach (var kvp in _sessions)
        {
            if (now - kvp.Value.CreatedAt <= ttl) continue;

            if (_sessions.TryRemove(kvp.Key, out var session))
            {
                try { session.Cts.Cancel(); }
                catch (ObjectDisposedException) { }
                cleaned++;
                Log.Debug("Expired media session removed: {SessionId}", kvp.Key);
            }
        }

        if (cleaned > 0)
        {
            Log.Information("MediaSession cleanup: removed {Count} expired session(s), {Remaining} active",
                cleaned, _sessions.Count);
        }
    }
}
