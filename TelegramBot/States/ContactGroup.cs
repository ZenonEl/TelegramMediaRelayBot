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


namespace MediaTelegramBot;

public class ProcessContactGroupState : IUserState
{
    public UsersStandardState currentState;

    public string groupInfo = "";

    private string callbackAction = "edit_contact_group";
    private string backCallback = "";
    private List<int> contactIDs = [];
    private int groupId = 0;
    private List<bool> isDBActionSuccessful = [];

    public ProcessContactGroupState()
    {
        currentState = UsersStandardState.ProcessAction;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

        if (!TelegramBot.userStates.TryGetValue(chatId, out IUserState? value))
        {
            return;
        }

        var userState = (ProcessContactGroupState)value;

        switch (userState.currentState)
        {
            case UsersStandardState.ProcessAction:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;

                bool? isUpdateSuccessful = await ProcessUpdate(botClient, update, cancellationToken);
                if (isUpdateSuccessful == true)
                {
                    userState.currentState = UsersStandardState.ProcessData;
                }
                else if (isUpdateSuccessful == null)
                {
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetReturnButtonMarkup(),
                        cancellationToken,
                        Config.GetResourceString("InputErrorMessage"));
                }

                break;

            case UsersStandardState.ProcessData:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;
                if (update.CallbackQuery != null && update.CallbackQuery.Data == backCallback)
                {
                    userState.currentState = UsersStandardState.ProcessAction;
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        $"{groupInfo}\n{Config.GetResourceString("ChooseOptionText")}");
                    return;
                }

                bool? isActionSuccessful = await ProcessAction(botClient, update, cancellationToken);

                if (isActionSuccessful == true) 
                {
                    userState.currentState = UsersStandardState.Finish;
                    return;
                }
                else if (isActionSuccessful == null)
                {
                    await botClient.SendMessage(chatId, Config.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetReturnButtonMarkup());
                    return;
                }
                break;

            case UsersStandardState.Finish:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;
                if (update.CallbackQuery != null && update.CallbackQuery.Data == backCallback)
                {
                    userState.currentState = UsersStandardState.ProcessAction;
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        $"{groupInfo}\n{Config.GetResourceString("ChooseOptionText")}");
                    return;
                }
                bool? isMessage = null;
                if (update.Message != null) isMessage = true;

                if (isMessage != true) 
                {
                    callbackAction = update.CallbackQuery!.Data!;
                    ProcessFinish(chatId);
                    string text = !isDBActionSuccessful.Contains(false) ? Config.GetResourceString("SuccessActionResult") : Config.GetResourceString("ErrorActionResult");
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken, text);
                    TelegramBot.userStates.Remove(chatId);
                    return;
                }
                await botClient.SendMessage(chatId, Config.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetReturnButtonMarkup());
                break;
        }
    }

    public async Task<bool?> ProcessUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            callbackAction = update.CallbackQuery!.Data!;
        }

        int userId = DBforGetters.GetUserIDbyTelegramID(Utils.Utils.GetIDfromUpdate(update));
        switch (callbackAction)
        {
            case "edit_contact_group":
                if (int.TryParse(update.Message!.Text!, out groupId) && DBforGroups.CheckGroupOwnership(groupId, userId))
                {
                    groupInfo = UsersGroup.GetUserGroupInfoByGroupId(groupId);

                    List<int> allContactsIds = DBforGroups.GetAllUsersIdsInGroup(groupId);
                    List<string> allContactsNames = [];

                    foreach (int contactId in allContactsIds)
                    {
                        allContactsNames.Add(DBforGetters.GetUserNameByID(contactId) + $" (ID: {contactId})");
                    }

                    string allContactsText;
                    allContactsText = $"{Config.GetResourceString("AllContactsText")} {string.Join("\n", allContactsNames)}";
                    
                    string messageText = $"{groupInfo}\n{allContactsText}\n{Config.GetResourceString("ChooseOptionText")}";
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        ContactGroup.GetContactGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        messageText);
                    return false;
                }
                return null;
            default:
                if (callbackAction.StartsWith("user_add_contact_to_group:"))
                {
                    groupId = int.Parse(callbackAction.Split(':')[1]);
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetReturnButton(),
                        cancellationToken,
                        Config.GetResourceString("InputContactIDsText"));
                    return true;
                }
                else if (callbackAction.StartsWith("user_remove_contact_from_group:"))
                {
                    groupId = int.Parse(callbackAction.Split(':')[1]);
                    await botClient.SendMessage(
                        Utils.Utils.GetIDfromUpdate(update),
                        Config.GetResourceString("InputContactIDsText"),
                        replyMarkup: KeyboardUtils.GetReturnButtonMarkup(),
                        cancellationToken: cancellationToken);
                    return true;
                }
                return null;
        }
    }

    public async Task<bool?> ProcessAction(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);

        try
        {
            if (callbackAction.StartsWith("user_add_contact_to_group:"))
            {
                groupId = int.Parse(callbackAction.Split(':')[1]);
                contactIDs = update.Message!.Text!.Split(" ").Select(x => int.Parse(x)).ToList();
                List<int> allowedIds = [];

                foreach (int contactId in contactIDs)
                {
                    bool status = DBforContactGroups.CheckUserAndContactConnect(userId, contactId);
                    if (status) allowedIds.Add(contactId);
                }

                if (allowedIds.Count == 0) return null;
                await Utils.Utils.SendMessage(
                    botClient,
                    update,
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_add_contact_to_group"),
                    cancellationToken,
                    Config.GetResourceString("ConfirmAddContactsToGroupText"));
                contactIDs = allowedIds;
                return true;
            }
            else if (callbackAction.StartsWith("user_remove_contact_from_group:"))
            {
                groupId = int.Parse(callbackAction.Split(':')[1]);
                contactIDs = update.Message!.Text!.Split(" ").Select(x => int.Parse(x)).ToList();
                await Utils.Utils.SendMessage(
                    botClient,
                    update,
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_delete_contact_from_group"),
                    cancellationToken,
                    Config.GetResourceString("ConfirmDeleteContactsFromGroupText"));
                return true;
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void ProcessFinish(long chatId)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);

        if (callbackAction.StartsWith("accept_add_contact_to_group"))
        {
            foreach (var contactId in contactIDs)
            {
                isDBActionSuccessful.Add(DBforContactGroups.AddContactToGroup(userId, contactId, groupId));
            }
        }
        else if (callbackAction.StartsWith("accept_delete_contact_from_group"))
        {
            foreach (var contactId in contactIDs)
            {
                isDBActionSuccessful.Add(DBforContactGroups.RemoveContactFromGroup(userId, contactId, groupId));
            }
        }
    }
}