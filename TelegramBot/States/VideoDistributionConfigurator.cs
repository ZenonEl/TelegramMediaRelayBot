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


namespace TelegramMediaRelayBot;

public class ProcessVideoDC : IUserState
{
    public UsersStandardState currentState;
    public string link { get; set; }
    public Message statusMessage { get; set; }
    public string text { get; set; }
    public CancellationTokenSource timeoutCTS { get; }
    public Queue<(string Link, string Text, int MessageId)> linkQueue = new Queue<(string Link, string Text, int MessageId)>();
    private string action = "";
    private List<long> targetUserIds = new List<long>();
    private List<long> preparedTargetUserIds = new List<long>();
    private readonly TGBot _tgBot;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IUserGetter _userGetter;

    public ProcessVideoDC(
        string Link,
        Message StatusMessage,
        string Text,
        CancellationTokenSource cts,
        TGBot tgBot,
        IContactGetter contactGetterRepository,
        IUserGetter userGetter
        )
    {
        link = Link;
        statusMessage = StatusMessage;
        text = Text;
        currentState = UsersStandardState.ProcessAction;
        timeoutCTS = cts;
        _tgBot = tgBot;
        _contactGetterRepository = contactGetterRepository;
        _userGetter = userGetter;
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
                    if (TGBot.userStates.TryGetValue(chatId, out var state) && state is ProcessVideoDC videoState)
                    {
                        videoState.timeoutCTS.Cancel();
                    }
                    string callbackData = update.CallbackQuery.Data!;
                    switch (callbackData)
                    {
                        case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                            action = UsersAction.SEND_MEDIA_TO_ALL_CONTACTS;
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;

                        case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                            action = UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS;
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;

                        case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                            action = UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS;
                            List<string> groupInfos = UsersGroup.GetUserGroupInfoByUserId(DBforGetters.GetUserIDbyTelegramID(chatId));

                            string messageText = groupInfos.Any() 
                                ? $"{Config.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
                                : Config.GetResourceString("AltYourGroupsText");
                            string text = $"{messageText}\n{Config.GetResourceString("PleaseEnterContactIDs")}";

                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, text, replyMarkup: KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                            currentState = UsersStandardState.ProcessData;
                            break;

                        case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                            action = UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS;
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken);
                            currentState = UsersStandardState.ProcessData;
                            break;

                        case UsersAction.SEND_MEDIA_ONLY_TO_ME:
                            action = UsersAction.SEND_MEDIA_ONLY_TO_ME;
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            currentState = UsersStandardState.Finish;
                            break;

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
                            Config.GetResourceString("VideoDistributionQuestion"), 
                            replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), 
                            replyParameters: new ReplyParameters { MessageId = replyToMessageId }, 
                            cancellationToken: cancellationToken
                        );
                        linkQueue.Enqueue((newLink, newText, statusMessage.MessageId));
                    }
                }
                break;

            case UsersStandardState.ProcessData:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu")
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("VideoDistributionQuestion"), replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), cancellationToken: cancellationToken);
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
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, Config.GetResourceString("InvalidInputNumbers"), cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        if (long.TryParse(input, out long id))
                        {
                            preparedTargetUserIds.Add(id);
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, Config.GetResourceString("InvalidInputNumbers"), cancellationToken: cancellationToken);
                        }
                    }
                }
                break;

            case UsersStandardState.Finish:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu" ||
                    update.Message != null)
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("VideoDistributionQuestion"), replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), cancellationToken: cancellationToken);
                    currentState = UsersStandardState.ProcessAction;
                    return;
                }

                await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);
                _ = _tgBot.HandleMediaRequest(botClient, link, chatId, statusMessage, targetUserIds, caption: text);

                if (linkQueue.Count > 0)
                {
                    var nextLink = linkQueue.Dequeue();
                    link = nextLink.Link;
                    text = nextLink.Text;
                    int replyToMessageId = nextLink.MessageId;
                    currentState = UsersStandardState.ProcessAction;

                    statusMessage = await botClient.SendMessage(
                        chatId, 
                        Config.GetResourceString("WaitDownloadingVideo"),
                        replyParameters: new ReplyParameters { MessageId = replyToMessageId }, 
                        cancellationToken: cancellationToken
                    );

                    await botClient.EditMessageText(
                        statusMessage.Chat.Id, 
                        statusMessage.MessageId, 
                        Config.GetResourceString("VideoDistributionQuestion"), 
                        replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), 
                        cancellationToken: cancellationToken
                    );

                    await ProcessState(botClient, update, cancellationToken);
                }
                else
                {
                    TGBot.userStates.Remove(chatId);
                }

                break;
        }
    }

    private async Task PrepareTargetUserIds(long chatId)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);
        List<long> contactUserTGIds = new List<long>();

        switch (action)
        {
            case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                contactUserTGIds = await _contactGetterRepository.GetAllContactUserTGIds(userId);
                targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                break;

            case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                List<int> defaultGroupContactIDs = DBforGroups.GetAllUsersInDefaultEnabledGroups(userId);

                targetUserIds = defaultGroupContactIDs
                    .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    .Select(DBforGetters.GetTelegramIDbyUserID)
                    .ToList();
                break;

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                List<int> contactUserIds = new List<int>();
                foreach (int groupId in preparedTargetUserIds)
                {
                    contactUserIds.AddRange(DBforGroups.GetAllUsersInGroup(groupId, userId));
                }

                targetUserIds = contactUserIds
                    .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    .Select(DBforGetters.GetTelegramIDbyUserID)
                    .ToList();
                    break;

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                foreach (int contactId in preparedTargetUserIds)
                {
                    contactUserTGIds.Add(DBforGetters.GetTelegramIDbyUserID(contactId));
                }
                List<long> allContactUserTGIds = await _contactGetterRepository.GetAllContactUserTGIds(userId);
                List<long> filteredContactUserTGIds = contactUserTGIds.Except(allContactUserTGIds).ToList();
                targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                break;
        }

        currentState = UsersStandardState.Finish;
    }
}