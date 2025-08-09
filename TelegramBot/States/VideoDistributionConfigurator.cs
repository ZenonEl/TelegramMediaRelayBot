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
    private readonly Dictionary<int, (string Link, string Text)> _pendingByMessageId = new();
    private readonly Dictionary<int, CancellationTokenSource> _sessionCtsByMessageId = new();
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
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService
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
            _pendingByMessageId[StatusMessage.MessageId] = (Link, Text);
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);

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
                    }
                    string callbackData = update.CallbackQuery.Data!;
                    switch (callbackData)
                    {
                        case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                            action = UsersAction.SEND_MEDIA_TO_ALL_CONTACTS;
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;

                        case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                            action = UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS;
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;

                        case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                            action = UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS;
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
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetReturnButtonMarkup());
                            currentState = UsersStandardState.ProcessData;
                            break;

                        case UsersAction.SEND_MEDIA_ONLY_TO_ME:
                            action = UsersAction.SEND_MEDIA_ONLY_TO_ME;
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            currentState = UsersStandardState.Finish;
                            break;

                        case "cancel_download":
                            // cancel only this message/session
                            if (_decisionCtsByMessageId.TryGetValue(statusMessage.MessageId, out var cancelCts))
                            {
                                try { cancelCts.Cancel(); } catch { }
                            }
                            if (_sessionCtsByMessageId.TryGetValue(statusMessage.MessageId, out var sessCts))
                            {
                                try { sessCts.Cancel(); } catch { }
                            }
                            _pendingByMessageId.Remove(statusMessage.MessageId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, _resourceService.GetResourceString("CanceledByUserMessage"), cancellationToken: cancellationToken);
                            return;

                        case "main_menu":
                            await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false);
                            break;
                    }
                }
                else if (update.Message != null && update.Message.Text != null)
                {
                    string messageText = update.Message.Text;
                    string newLink;
                    string newText = "";

                    int newLineIndex = messageText.IndexOf('\n');
                    if (newLineIndex != -1)
                    {
                        newLink = messageText[..newLineIndex].Trim();
                        newText = messageText[(newLineIndex + 1)..].Trim();
                    }
                    else
                    {
                        newLink = messageText.Trim();
                    }

                    if (CommonUtilities.IsLink(newLink))
                    {

                        int replyToMessageId = update.Message.MessageId;
                        statusMessage = await botClient.SendMessage(
                            chatId, 
                            _resourceService.GetResourceString("VideoDistributionQuestion"), 
                            replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), 
                            replyParameters: new ReplyParameters { MessageId = replyToMessageId }, 
                            cancellationToken: cancellationToken
                        );
                        _pendingByMessageId[statusMessage.MessageId] = (newLink, newText);
                        // schedule default action for this message id if configured
                        await TryScheduleDefaultActionForMessage(botClient, chatId, statusMessage, newLink, newText, cancellationToken);
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
                _ = _tgBot.HandleMediaRequest(botClient, link, chatId, statusMessage, targetUserIds, groupChat: false, caption: text, sessionToken: sessionCts.Token);
                _pendingByMessageId.Remove(statusMessage.MessageId);
                currentState = UsersStandardState.ProcessAction;

                break;
        }
    }

    private async Task TryScheduleDefaultActionForMessage(ITelegramBotClient botClient, long chatId, Message status, string url, string initialText, CancellationToken cancellationToken)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        string defaultActionData = _defaultActionGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        if (defaultActionData == UsersAction.NO_VALUE) return;
        string defaultAction = defaultActionData.Split(';')[0];
        int defaultCondition = int.Parse(defaultActionData.Split(';')[1]);
        if (defaultAction == UsersAction.OFF) return;
        var cts = new CancellationTokenSource();
        _decisionCtsByMessageId[status.MessageId] = cts;
        var privateUtils = new PrivateUtils(_tgBot, _contactGetterRepository, _defaultActionGetter, _userGetter, _groupGetter, _resourceService);
        privateUtils.ProcessDefaultSendAction(botClient, chatId, status, defaultAction, cancellationToken, userId, defaultCondition, cts, url, initialText);
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
}