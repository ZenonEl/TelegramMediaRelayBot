using System.Collections.Concurrent;

namespace TelegramMediaRelayBot.TelegramBot.Sessions;

/// <summary>
/// Управляет жизненным циклом всех активных сессий обработки ссылок.
/// Является Singleton сервисом.
/// </summary>
public class DownloadSessionManager
{
    private readonly ConcurrentDictionary<int, DownloadSession> _sessions = new();

    /// <summary>
    /// Создает и регистрирует новую сессию.
    /// </summary>
    public DownloadSession CreateSession(int statusMessageId, long chatId, string url, string caption, DateTime originalMessageDateUtc)
    {
        var session = new DownloadSession
        {
            StatusMessageId = statusMessageId,
            ChatId = chatId,
            Url = url,
            Caption = caption,
            OriginalMessageDateUtc = originalMessageDateUtc
        };

        _sessions[statusMessageId] = session;
        Log.Debug("Created download session for message {MessageId}", statusMessageId);
        return session;
    }

    public bool TryGetSession(int statusMessageId, out DownloadSession? session)
    {
        return _sessions.TryGetValue(statusMessageId, out session);
    }

    /// <summary>
    /// Отменяет таймер действия по умолчанию для сессии.
    /// </summary>
    public void CancelDefaultAction(int statusMessageId)
    {
        if (_sessions.TryGetValue(statusMessageId, out var session) && session.DefaultActionCts != null)
        {
            try { session.DefaultActionCts.Cancel(); } catch { }
            session.DefaultActionCts.Dispose();
            session.DefaultActionCts = null;
            Log.Debug("Canceled default action for session {MessageId}", statusMessageId);
        }
    }

    /// <summary>
    /// Отменяет всю сессию (скачивание/отправку).
    /// </summary>
    public bool CancelSession(int statusMessageId)
    {
        if (_sessions.TryGetValue(statusMessageId, out var session))
        {
            try { session.SessionCts.Cancel(); } catch { }
            Log.Information("Canceled session {MessageId} by user request.", statusMessageId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Завершает и удаляет сессию из памяти.
    /// </summary>
    public void CompleteSession(int statusMessageId)
    {
        if (_sessions.TryRemove(statusMessageId, out var session))
        {
            session.DefaultActionCts?.Dispose();
            session.SessionCts.Dispose();
            Log.Debug("Completed and removed session for message {MessageId}", statusMessageId);
        }
    }

    // TODO: Добавить метод CleanupStaleSessions, который будет вызываться из Scheduler
}