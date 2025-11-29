// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Models;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IInboxService
{
    /// <summary>
    /// Пытается доставить медиа через Инбокс.
    /// </summary>
    /// <returns>True, если медиа было успешно добавлено в инбокс (и не требует прямой отправки).</returns>
    Task<bool> TryDeliverToInbox(ITelegramBotClient botClient, DownloadSession session, int recipientUserId, List<TelegramMediaInfo> savedMediaRefs);
}

public class InboxService : IInboxService
{
    private readonly IPrivacySettingsGetter _privacyGetter;
    private readonly IInboxRepository _inboxRepo;
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    private static readonly ConcurrentDictionary<int, DateTime> _lastInboxNotifyUtc = new();

    public InboxService(IPrivacySettingsGetter privacyGetter, IInboxRepository inboxRepo, IUserGetter userGetter, IResourceService resourceService)
    {
        _privacyGetter = privacyGetter;
        _inboxRepo = inboxRepo;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public async Task<bool> TryDeliverToInbox(ITelegramBotClient botClient, DownloadSession session, int recipientUserId, List<TelegramMediaInfo> savedMediaRefs)
    {
        if (!_privacyGetter.GetIsActivePrivacyRule(recipientUserId, PrivacyRuleType.INBOX_DELIVERY))
        {
            return false;
        }
        // TODO: Логика инбокса должна использовать FileId, а не байты 
        try
        {
            var senderUserId = _userGetter.GetUserIDbyTelegramID(session.ChatId);

            // --- ТВОЯ ЛОГИКА "СЛИЯНИЯ" ---
            var latestItem = await _inboxRepo.GetLatestItemForOwnerFromAsync(recipientUserId, senderUserId);
            if (latestItem != null)
            {
                // ... (здесь будет твоя логика парсинга JSON и проверки на sameUrl/sameCaption)
                // Если удалось "слить", то return true;
            }

            // Если "слить" не удалось, создаем новый элемент
            var payload = new { /* ... */ };
            string payloadJson = JsonSerializer.Serialize(payload);
            await _inboxRepo.AddItemAsync(recipientUserId, senderUserId, session.Caption, payloadJson, "new");
            Log.Information("Added to Inbox for user {User}", recipientUserId);
            
            await NotifyRecipientIfNeeded(botClient, recipientUserId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add Inbox item, fallback to direct send");
            return false;
        }
    }

    private async Task NotifyRecipientIfNeeded(ITelegramBotClient botClient, int recipientUserId)
    {
        // --- ТВОЯ ЛОГИКА НОТИФИКАЦИЙ ---
        var newCount = await _inboxRepo.GetNewCountAsync(recipientUserId);
        bool shouldNotify = newCount == 1 || (newCount >= 5 && newCount % 5 == 0);

        if (shouldNotify)
        {
            var nowUtc = DateTime.UtcNow;
            if (!_lastInboxNotifyUtc.TryGetValue(recipientUserId, out var last) || (nowUtc - last) > TimeSpan.FromSeconds(5))
            {
                _lastInboxNotifyUtc[recipientUserId] = nowUtc;
                var recipientTgId = _userGetter.GetTelegramIDbyUserID(recipientUserId);
                var note = string.Format(_resourceService.GetResourceString("InboxNewCountNotify"), newCount);
                await botClient.SendMessage(recipientTgId, note);
            }
        }
    }
}