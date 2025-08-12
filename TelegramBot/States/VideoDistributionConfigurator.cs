// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers;


namespace TelegramMediaRelayBot;

    public class ProcessVideoDC : IUserState
{
    public UsersStandardState currentState;
        public string link { get; set; }
        public Message statusMessage { get; set; }
        public string text { get; set; }
        // per-message default-action timers
        private readonly Dictionary<int, CancellationTokenSource> _decisionCtsByMessageId = new();
        // pending entries per message
    private readonly Dictionary<int, (string Link, string Text, DateTime CreatedAt)> _pendingByMessageId = new();
    private readonly Dictionary<int, CancellationTokenSource> _sessionCtsByMessageId = new();
    private readonly TelegramMediaRelayBot.TelegramBot.Utils.ITextCleanupService _textCleanup;
    private string action = "";
    private List<long> targetUserIds = new List<long>();
    private List<long> preparedTargetUserIds = new List<long>();
    private readonly TGBot _tgBot;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

        public ProcessVideoDC(
            string Link,
            Message StatusMessage,
            string Text,
        TGBot tgBot,
        IContactGetter contactGetterRepository,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
            IDefaultActionGetter defaultActionGetter,
            TelegramMediaRelayBot.Config.Services.IResourceService resourceService,
            TelegramMediaRelayBot.TelegramBot.Utils.ITextCleanupService textCleanup
        )
    {
        link = Link;
        statusMessage = StatusMessage;
        text = Text;
        currentState = UsersStandardState.ProcessAction;
        _tgBot = tgBot;
        _contactGetterRepository = contactGetterRepository;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
            _defaultActionGetter = defaultActionGetter;
        _resourceService = resourceService;
            _textCleanup = textCleanup;
            _pendingByMessageId[StatusMessage.MessageId] = (Link, Text, DateTime.UtcNow);
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public string GetPendingTextOrCurrent(int messageId)
    {
        if (_pendingByMessageId.TryGetValue(messageId, out var entry))
        {
            return entry.Text;
        }
        return text;
    }

    // Schedules default action for the provided message immediately (used for the very first link)
    public Task ScheduleDefaultActionFor(ITelegramBotClient botClient, long chatId, Message status, string url, string initialText, CancellationToken cancellationToken)
    {
        return TryScheduleDefaultActionForMessage(botClient, chatId, status, url, initialText, cancellationToken);
    }

    // Guard: check if a manual/active session already started for this message
    public bool IsSessionActiveForMessage(int messageId)
    {
        return _sessionCtsByMessageId.ContainsKey(messageId);
    }

    // Guard: disable any scheduled auto action for this message id
    public void DisableAutoForMessageId(int messageId)
    {
        if (_decisionCtsByMessageId.TryGetValue(messageId, out var cts))
        {
            try { cts.Cancel(); } catch { }
            _decisionCtsByMessageId.Remove(messageId);
        }
    }

    // Cancel all timers (auto and session) and clear pending state
    public void CancelAll()
    {
        foreach (var kv in _decisionCtsByMessageId.Values)
        {
            try { kv.Cancel(); } catch { }
        }
        foreach (var kv in _sessionCtsByMessageId.Values)
        {
            try { kv.Cancel(); } catch { }
        }
        _decisionCtsByMessageId.Clear();
        _sessionCtsByMessageId.Clear();
        _pendingByMessageId.Clear();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);

        // Глобальный выход из стейта по /start или main_menu из ЛЮБОГО состояния
        if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false))
        {
            // Отменяем таймеры для всех pending текущего чата
            foreach (var kv in _decisionCtsByMessageId.Values)
            {
                try { kv.Cancel(); } catch { }
            }
            foreach (var kv in _sessionCtsByMessageId.Values)
            {
                try { kv.Cancel(); } catch { }
            }
            _pendingByMessageId.Clear();
            return;
        }

        // TTL авто-уборка висячих сессий
        CleanupStalePending(TimeSpan.FromMinutes(10));

        switch (currentState)
        {
            case UsersStandardState.ProcessAction:
                if (update.CallbackQuery != null)
                {
                    // pivot to the message which generated the callback
                    if (update.CallbackQuery.Message != null)
                    {
                        statusMessage = update.CallbackQuery.Message;
                        if (_decisionCtsByMessageId.TryGetValue(statusMessage.MessageId, out var cts))
                        {
                            try { cts.Cancel(); } catch { }
                        }
                        if (_pendingByMessageId.TryGetValue(statusMessage.MessageId, out var entry))
                        {
                            link = entry.Link;
                            text = entry.Text;
                        }
                        else
                        {
                            // нет pending для этого сообщения — не продолжаем, чтобы не перепутать ссылки
                            return;
                        }
                    }
                    string callbackData = update.CallbackQuery.Data!;
                    // поддержка формата "cmd:messageId" — отбрасываем часть после второй ':'
                    var cbParts = callbackData.Split(':');
                    if (cbParts.Length > 1 && int.TryParse(cbParts[^1], out int cbMsgId))
                    {
                        if (statusMessage.MessageId != cbMsgId)
                        {
                            return; // чужой колбек
                        }
                        callbackData = string.Join(':', cbParts.Take(cbParts.Length - 1));
                    }
                    switch (callbackData)
                    {
                        case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                            action = UsersAction.SEND_MEDIA_TO_ALL_CONTACTS;
                            // cancel and remove any auto timer for this message before proceeding
                            DisableAutoForMessageId(statusMessage.MessageId);
                            await PrepareTargetUserIds(chatId);
                        await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            currentState = UsersStandardState.Finish;
                            break;

                        case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                            action = UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS;
                            DisableAutoForMessageId(statusMessage.MessageId);
                            await PrepareTargetUserIds(chatId);
                        await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            currentState = UsersStandardState.Finish;
                            break;

                        case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                            action = UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS;
                            DisableAutoForMessageId(statusMessage.MessageId);
                            List<string> groupInfos = await UsersGroup.GetUserGroupInfoByUserId(_userGetter.GetUserIDbyTelegramID(chatId), _groupGetter);

                            string messageText = groupInfos.Any() 
                                ? $"{_resourceService.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
                                : _resourceService.GetResourceString("AltYourGroupsText");
                            string text = $"{messageText}\n{_resourceService.GetResourceString("PleaseEnterContactIDs")}";

                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, text, replyMarkup: KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                            currentState = UsersStandardState.ProcessData;
                            break;

                        case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                            action = UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS;
                            DisableAutoForMessageId(statusMessage.MessageId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetReturnButtonMarkup());
                            currentState = UsersStandardState.ProcessData;
                            break;

                        case UsersAction.SEND_MEDIA_ONLY_TO_ME:
                            action = UsersAction.SEND_MEDIA_ONLY_TO_ME;
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            currentState = UsersStandardState.Finish;
                            break;

                        case "cancel_download":
                            // Полная отмена: останавливаем таймеры и выходим из состояния
                            foreach (var cts in _decisionCtsByMessageId.Values)
                            {
                                try { cts.Cancel(); } catch { }
                            }
                            foreach (var cts in _sessionCtsByMessageId.Values)
                            {
                                try { cts.Cancel(); } catch { }
                            }
                            _decisionCtsByMessageId.Clear();
                            _sessionCtsByMessageId.Clear();
                            _pendingByMessageId.Clear();
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("CanceledByUserMessage"), cancellationToken: cancellationToken);
                            TGBot.StateManager.Remove(chatId);
                            return;

                        case "main_menu":
                            // Уже обрабатывается в глобальном pre-check в начале метода, сюда не должны доходить
                            return;
                    }
                }
                else if (update.Message != null && update.Message.Text != null)
                {
                    string messageText = update.Message.Text;
                    string newLink;
                    string newText;
                    if (!CommonUtilities.TryExtractLinkAndText(messageText, out newLink, out newText))
                    {
                        newLink = string.Empty;
                        newText = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(newLink))
                    {
                        // Подхватить предыдущий текст, если он был недавно
                        var userId = _userGetter.GetUserIDbyTelegramID(chatId);
                        var defaultActionData = _defaultActionGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
                        int baseDelaySec = 0;
                        if (defaultActionData != UsersAction.NO_VALUE)
                        {
                            var parts = defaultActionData.Split(';');
                            if (parts.Length >= 2 && int.TryParse(parts[1], out var s)) baseDelaySec = s;
                        }
                        if (string.IsNullOrWhiteSpace(newText))
                        {
                            var last = TGBot.TryGetLastText(chatId);
                            if (last.Found && (DateTime.UtcNow - last.At) <= TimeSpan.FromSeconds(baseDelaySec))
                            {
                                var domain = CommonUtilities.ExtractDomain(newLink);
                                newText = _textCleanup.Cleanup(last.Text, domain);
                            }
                        }
                        int replyToMessageId = update.Message.MessageId;
                        statusMessage = await botClient.SendMessage(
                            chatId, 
                            _resourceService.GetResourceString("VideoDistributionQuestion"), 
                            replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), 
                            replyParameters: new ReplyParameters { MessageId = replyToMessageId }, 
                            cancellationToken: cancellationToken
                        );
                        // Очистим подпись по домену уже на этапе pending
                        var cleanedInitial = string.IsNullOrWhiteSpace(newText) ? newText : _textCleanup.Cleanup(newText, CommonUtilities.ExtractDomain(newLink));
                        _pendingByMessageId[statusMessage.MessageId] = (newLink, cleanedInitial, DateTime.UtcNow);
                        // schedule default action for this message id if configured
                        await TryScheduleDefaultActionForMessage(botClient, chatId, statusMessage, newLink, newText, cancellationToken);
                    }
                    else
                    {
                        // не ссылка -> запоминаем как предыдущий текст
                        TGBot.RememberLastText(chatId, messageText);
                        // Если есть активный pending и окно ещё не истекло — считаем это next-caption и заменяем его
                        if (_pendingByMessageId.Count > 0)
                        {
                            // берём последний созданный pending
                            var kv = _pendingByMessageId.OrderByDescending(p => p.Value.CreatedAt).First();
                            var pending = kv.Value;
                            var userId2 = _userGetter.GetUserIDbyTelegramID(chatId);
                            var da = _defaultActionGetter.GetDefaultActionByUserIDAndType(userId2, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
                            int baseDelay2 = 0;
                            if (da != UsersAction.NO_VALUE)
                            {
                                var parts = da.Split(';');
                                if (parts.Length >= 2 && int.TryParse(parts[1], out var s)) baseDelay2 = s;
                            }
                            if (baseDelay2 <= 0) baseDelay2 = 5; // минимальное окно 5 секунд
                            if ((DateTime.UtcNow - pending.CreatedAt) <= TimeSpan.FromSeconds(baseDelay2))
                            {
                                // Обновляем подпись на текст из следующего сообщения как есть (без очистки)
                                _pendingByMessageId[kv.Key] = (pending.Link, messageText, pending.CreatedAt);
                                // Перепланируем авто-действие под обновлённый текст
                                if (_decisionCtsByMessageId.TryGetValue(kv.Key, out var oldCts))
                                {
                                    try { oldCts.Cancel(); } catch { }
                                    _decisionCtsByMessageId.Remove(kv.Key);
                                }
                                // Переиспользуем текущее statusMessage если совпадает, иначе создаём минимальный объект с нужным ChatId и MessageId через конструктор ReplyParameters
                                var status = statusMessage?.MessageId == kv.Key ? statusMessage : new Message();
                                status = statusMessage ?? new Message();
                                status.Chat = new Chat { Id = chatId };
                                // Передаём оригинальный statusMessage, если он есть, иначе используем текущий
                                _ = TryScheduleDefaultActionForMessage(botClient, chatId, statusMessage ?? status, pending.Link, messageText, cancellationToken);
                            }
                        }
                    }
                }
                break;

            case UsersStandardState.ProcessData:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu")
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("VideoDistributionQuestion"), replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), cancellationToken: cancellationToken);
                    currentState = UsersStandardState.ProcessAction;
                    return;
                }
                else if (update.Message != null)
                {
                    string input = update.Message.Text!;
                    if (input.Contains(" "))
                    {
                        string[] ids = input.Split(' ');
                        if (ids.All(id => long.TryParse(id, out _)))
                        {
                            preparedTargetUserIds = ids.Select(long.Parse).ToList();
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        if (long.TryParse(input, out long id))
                        {
                            preparedTargetUserIds.Add(id);
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                        }
                    }
                }
                break;

            case UsersStandardState.Finish:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu" ||
                    update.Message != null)
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("VideoDistributionQuestion"), replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), cancellationToken: cancellationToken);
                    currentState = UsersStandardState.ProcessAction;
                    return;
                }

                await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetCancelKeyboardMarkup());
                // start download/send specifically for this message
                var sessionCts = new CancellationTokenSource();
                _sessionCtsByMessageId[statusMessage.MessageId] = sessionCts;
                // ensure any auto timer is disabled now that manual session started
                DisableAutoForMessageId(statusMessage.MessageId);
                // Очередь задержек: учитываем базовую задержку как окно и как слот
                var da2 = _defaultActionGetter.GetDefaultActionByUserIDAndType(_userGetter.GetUserIDbyTelegramID(chatId), UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
                int baseDelay = 0; if (da2 != UsersAction.NO_VALUE) { var parts = da2.Split(';'); if (parts.Length >= 2 && int.TryParse(parts[1], out var s)) baseDelay = s; }
                var wait = _tgBot.ReserveDelaySlot(chatId, TimeSpan.FromSeconds(baseDelay));
                try { await Task.Delay(wait, sessionCts.Token); } catch { }
                // Очистка caption по домену ссылки перед отправкой; берём pending-текст, если есть
                var captionRaw = _pendingByMessageId.TryGetValue(statusMessage.MessageId, out var pend) ? pend.Text : text;
                string cleanedText = captionRaw;
                try
                {
                    if (!string.IsNullOrWhiteSpace(captionRaw) && Uri.TryCreate(link, UriKind.Absolute, out var uri))
                    {
                        cleanedText = _textCleanup.Cleanup(captionRaw, uri.Host);
                    }
                }
                catch { }
                _ = _tgBot.HandleMediaRequest(botClient, link, chatId, statusMessage, targetUserIds, groupChat: false, caption: cleanedText, sessionToken: sessionCts.Token);
                _pendingByMessageId.Remove(statusMessage.MessageId);
                currentState = UsersStandardState.ProcessAction;

                break;
        }
    }

    private Task TryScheduleDefaultActionForMessage(ITelegramBotClient botClient, long chatId, Message status, string url, string initialText, CancellationToken cancellationToken)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        string defaultActionData = _defaultActionGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        if (defaultActionData == UsersAction.NO_VALUE) return Task.CompletedTask;
        string defaultAction = defaultActionData.Split(';')[0];
        int defaultCondition = 0;
        var partsDelay = defaultActionData.Split(';');
        if (partsDelay.Length >= 2) int.TryParse(partsDelay[1], out defaultCondition);
        if (defaultCondition <= 0) defaultCondition = 5; // дефолтная задержка окна/таймера, если не задано
        if (defaultAction == UsersAction.OFF) return Task.CompletedTask;
        var cts = new CancellationTokenSource();
        _decisionCtsByMessageId[status.MessageId] = cts;
        var privateUtils = new PrivateUtils(_tgBot, _contactGetterRepository, _defaultActionGetter, _userGetter, _groupGetter, _resourceService);
        privateUtils.ProcessDefaultSendAction(botClient, chatId, status, defaultAction, cancellationToken, userId, defaultCondition, cts, url, initialText);
        return Task.CompletedTask;
    }

    public void RegisterSession(long messageId, CancellationTokenSource sessionCts)
    {
        _sessionCtsByMessageId[(int)messageId] = sessionCts;
    }

    private async Task PrepareTargetUserIds(long chatId)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = await _userGetter.GetUsersIdForMuteContactIdAsync(userId);
        List<long> contactUserTGIds = new List<long>();

        switch (action)
        {
            case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                contactUserTGIds = await _contactGetterRepository.GetAllContactUserTGIds(userId);
                targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                break;

            case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                List<int> defaultGroupContactIDs = await _groupGetter.GetAllUsersInDefaultEnabledGroups(userId);

                targetUserIds = defaultGroupContactIDs
                    .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                    .Select(_userGetter.GetTelegramIDbyUserID)
                    .ToList();
                break;

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                List<int> contactUserIds = new List<int>();
                foreach (int groupId in preparedTargetUserIds)
                {
                    contactUserIds.AddRange(await _groupGetter.GetAllUsersInGroup(groupId, userId));
                }

                targetUserIds = contactUserIds
                    .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                    .Select(_userGetter.GetTelegramIDbyUserID)
                    .ToList();
                    break;

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                foreach (int contactId in preparedTargetUserIds)
                {
                    contactUserTGIds.Add(_userGetter.GetTelegramIDbyUserID(contactId));
                }
                List<long> allContactUserTGIds = await _contactGetterRepository.GetAllContactUserTGIds(userId);
                List<long> filteredContactUserTGIds = contactUserTGIds.Except(allContactUserTGIds).ToList();
                targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                break;
        }

        currentState = UsersStandardState.Finish;
    }

    private void CleanupStalePending(TimeSpan ttl)
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _pendingByMessageId.ToList())
        {
            if (now - kv.Value.CreatedAt > ttl)
            {
                if (_decisionCtsByMessageId.TryGetValue(kv.Key, out var dcts)) { try { dcts.Cancel(); } catch { } _decisionCtsByMessageId.Remove(kv.Key); }
                if (_sessionCtsByMessageId.TryGetValue(kv.Key, out var scts)) { try { scts.Cancel(); } catch { } _sessionCtsByMessageId.Remove(kv.Key); }
                _pendingByMessageId.Remove(kv.Key);
            }
        }
    }
}