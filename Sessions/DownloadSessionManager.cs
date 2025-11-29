// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Sessions;

/// <summary>
/// Управляет жизненным циклом всех активных сессий обработки ссылок.
/// Является Singleton сервисом.
/// </summary>
public class DownloadSessionManager
{
    private readonly ConcurrentDictionary<int, DownloadSession> _sessions = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITextCleanupService _textCleaner;

    public DownloadSessionManager(IServiceScopeFactory scopeFactory, ITextCleanupService textCleaner)
    {
        _scopeFactory = scopeFactory;
        _textCleaner = textCleaner;
    }

    /// <summary>
    /// Создает и регистрирует новую сессию.
    /// </summary>
    public DownloadSession CreateSession(int statusMessageId, long chatId, string url, string caption, DateTime originalMessageDateUtc)
    {
        DownloadSession session = new DownloadSession
        {
            StatusMessageId = statusMessageId,
            ChatId = chatId,
            Url = url,
            Caption = _textCleaner.Cleanup(caption), 
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

    public DownloadSession? GetLatestPendingSession(long chatId)
    {
        return _sessions.Values
            .Where(s => s.ChatId == chatId && !s.IsProcessing)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefault();
    }

    public void UpdateCaption(int statusMessageId, string newCaption)
    {
        if (_sessions.TryGetValue(statusMessageId, out var session))
        {
            session.Caption = _textCleaner.Cleanup(newCaption);
            Log.Debug("Updated caption for session {MessageId}", statusMessageId);
        }
    }

    /// <summary>
    /// Отменяет таймер действия по умолчанию для сессии.
    /// </summary>
    public void CancelDefaultAction(int statusMessageId)
    {
        if (_sessions.TryGetValue(statusMessageId, out DownloadSession session) && session.DefaultActionCts != null)
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
        if (_sessions.TryGetValue(statusMessageId, out DownloadSession session))
        {
            try { session.SessionCts.Cancel(); } catch { }
            Log.Information("Canceled session {MessageId} by user request.", statusMessageId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Помечает сессию как "в обработке", чтобы на нее больше не влияли новые сообщения.
    /// </summary>
    public void MarkAsProcessing(int statusMessageId)
    {
        if (_sessions.TryGetValue(statusMessageId, out var session))
        {
            session.IsProcessing = true;
            Log.Debug("Session {MessageId} is now marked as processing.", statusMessageId);
        }
    }

    /// <summary>
    /// Завершает и удаляет сессию из памяти.
    /// </summary>
    public void CompleteSession(int statusMessageId)
    {
        if (_sessions.TryRemove(statusMessageId, out DownloadSession session))
        {
            session.DefaultActionCts?.Dispose();
            session.SessionCts.Dispose();
            Log.Debug("Completed and removed session for message {MessageId}", statusMessageId);
        }
    }

    /// <summary>
    /// Запускает таймер для выполнения действия по умолчанию для сессии.
    /// </summary>
    public void ScheduleDefaultAction(ITelegramBotClient botClient, Update update, DownloadSession session)
    {
        session.DefaultActionCts = new CancellationTokenSource();
        CancellationToken cancellationToken = session.DefaultActionCts.Token;

        _ = Task.Run(async () =>
        {
            await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
            {
                IDefaultActionGetter defaultActionGetter = scope.ServiceProvider.GetRequiredService<IDefaultActionGetter>();
                IUserGetter userGetter = scope.ServiceProvider.GetRequiredService<IUserGetter>();
                IMediaProcessingFlow mediaFlow = scope.ServiceProvider.GetRequiredService<IMediaProcessingFlow>();

                try
                {
                    int userId = userGetter.GetUserIDbyTelegramID(session.ChatId);
                    string defaultActionData = defaultActionGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);

                    if (defaultActionData == UsersAction.NO_VALUE || defaultActionData == UsersAction.OFF)
                    {
                        return;
                    }

                    string[] parts = defaultActionData.Split(';');
                    string action = parts[0];
                    if (!int.TryParse(parts.Length > 1 ? parts[1] : "5", out int delaySeconds))
                    {
                        delaySeconds = 5;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

                    await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Processing default action...", cancellationToken: cancellationToken);

                    List<long> targetUserIds = await GetDefaultActionTargets(userGetter, scope.ServiceProvider, userId, action);

                    await mediaFlow.StartFlow(botClient, update, session, targetUserIds);
                }
                catch (OperationCanceledException)
                {
                    Log.Debug("Default action for session {MessageId} was canceled.", session.StatusMessageId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error executing default action for session {MessageId}", session.StatusMessageId);
                }
            }
        }, cancellationToken);
    }

    private async Task<List<long>> GetDefaultActionTargets(IUserGetter userGetter, IServiceProvider sp, int userId, string action)
    {
        List<int> userIds;
        int actionId;
        switch (action)
        {
            case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                IContactGetter contactGetter = sp.GetRequiredService<IContactGetter>();
                return await contactGetter.GetAllContactUserTGIds(userId);

            case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                IGroupGetter groupGetter = sp.GetRequiredService<IGroupGetter>();
                userIds = await groupGetter.GetAllUsersInDefaultEnabledGroups(userId);
                return userIds.Select(userGetter.GetTelegramIDbyUserID).ToList();

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                IDefaultActionGetter contactGetterSpg = sp.GetRequiredService<IDefaultActionGetter>();
                actionId = await contactGetterSpg.GetDefaultActionIdAsync(userId, UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS);
                userIds = await contactGetterSpg.GetAllDefaultUsersActionTargetsAsync(userId, UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS, actionId);
                return userIds.Select(userGetter.GetTelegramIDbyUserID).ToList();

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                IDefaultActionGetter contactGetterSpu = sp.GetRequiredService<IDefaultActionGetter>();
                actionId = await contactGetterSpu.GetDefaultActionIdAsync(userId, UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS);
                userIds = await contactGetterSpu.GetAllDefaultUsersActionTargetsAsync(userId, UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS, actionId);
                return userIds.Select(userGetter.GetTelegramIDbyUserID).ToList();

            default:
                return new List<long>();
        }
    }

    // TODO: Добавить метод CleanupStaleSessions, который будет вызываться из Scheduler
}