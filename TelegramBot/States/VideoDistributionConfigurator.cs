// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot;
using Telegram.Bot.Types;
using DataBase;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;
using Telegram.Bot.Types.Enums;


namespace MediaTelegramBot;

public class ProcessVideoDC : IUserState
{
    public UsersStandartState currentState;
    public string link { get; set; }
    public Message statusMessage { get; set; }
    public string text { get; set; }
    private string action = "";
    private List<long> targetUserIds = new List<long>();
    private List<long> preparedTargetUserIds = new List<long>();

    public ProcessVideoDC(string Link, Message StatusMessage, string Text)
    {
        link = Link;
        statusMessage = StatusMessage;
        text = Text;
        currentState = UsersStandartState.ProcessAction;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.Utils.GetIDfromUpdate(update);

        switch (currentState)
        {
            case UsersStandartState.ProcessAction:
                if (update.CallbackQuery != null)
                {
                    string callbackData = update.CallbackQuery.Data!;
                    switch (callbackData)
                    {
                        case "send_to_all_contacts":
                            action = "send_to_all_contacts";
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;

                        case "send_to_default_groups":
                            action = "send_to_default_groups";
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;

                        case "send_to_specified_groups":
                            action = "send_to_specified_groups";
                            List<string> groupInfos = UsersGroup.GetUserGroupInfoByUserId(DBforGetters.GetUserIDbyTelegramID(chatId));

                            string messageText = groupInfos.Any() 
                                ? $"{Config.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
                                : Config.GetResourceString("AltYourGroupsText");
                            string text = $"{messageText}\n{Config.GetResourceString("PleaseEnterContactIDs")}";

                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, text, replyMarkup: KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                            currentState = UsersStandartState.ProcessData;
                            break;

                        case "send_to_specified_users":
                            action = "send_to_specified_users";
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken);
                            currentState = UsersStandartState.ProcessData;
                            break;

                        case "send_only_to_me":
                            action = "send_only_to_me";
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            currentState = UsersStandartState.Finish;
                            break;

                        case "main_menu":
                            await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false);
                            break;
                    }
                }
                break;

            case UsersStandartState.ProcessData:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu")
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("VideoDistributionQuestion"), replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), cancellationToken: cancellationToken);
                    currentState = UsersStandartState.ProcessAction;
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

            case UsersStandartState.Finish:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu" ||
                    update.Message != null)
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("VideoDistributionQuestion"), replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), cancellationToken: cancellationToken);
                    currentState = UsersStandartState.ProcessAction;
                    return;
                }

                await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);
                TelegramBot.userStates.Remove(chatId);
                _ = TelegramBot.HandleVideoRequest(botClient, link, chatId, statusMessage, targetUserIds, caption: text);
                break;
        }
    }

    private async Task PrepareTargetUserIds(long chatId)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = DBforGetters.GetUsersIdForMuteContactId(userId);
        List<long> contactUserTGIds = new List<long>();

        switch (action)
        {
            case "send_to_all_contacts":
                contactUserTGIds = await CoreDB.GetAllContactUserTGIds(userId);
                targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                break;

            case "send_to_default_groups":
                List<int> defaultGroupContactIDs = DBforGroups.GetAllUsersInDefaultEnabledGroups(userId);

                targetUserIds = defaultGroupContactIDs
                    .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    .Select(DBforGetters.GetTelegramIDbyUserID)
                    .ToList();
                break;

            case "send_to_specified_groups":
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

            case "send_to_specified_users":
                foreach (int contactId in preparedTargetUserIds)
                {
                    contactUserTGIds.Add(DBforGetters.GetTelegramIDbyUserID(contactId));
                }
                List<long> allContactUserTGIds = await CoreDB.GetAllContactUserTGIds(userId);
                List<long> filteredContactUserTGIds = contactUserTGIds.Except(allContactUserTGIds).ToList();
                targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                break;
        }

        currentState = UsersStandartState.Finish;
    }
}