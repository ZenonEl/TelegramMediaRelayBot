// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;


namespace TelegramMediaRelayBot;

/// <summary>
/// Edits user-defined contact groups: add/remove members. Uses inline flows and supports /start bailout.
/// </summary>
public class ProcessContactGroupState : IUserState
{
    public UsersStandardState currentState;

    public string groupInfo = "";

    private string callbackAction = "edit_contact_group";
    private string backCallback = "";
    private List<int> contactIDs = [];
    private int groupId = 0;
    private List<bool> isDBActionSuccessful = [];
    private IContactGroupRepository _contactGroupRepository;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IContactGetter _contactGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public ProcessContactGroupState(
        IContactGroupRepository contactGroupRepository,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        IContactGetter contactGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService
        )
    {
        currentState = UsersStandardState.ProcessAction;
        _contactGroupRepository = contactGroupRepository;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _contactGetter = contactGetter;
        _resourceService = resourceService;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    /// <summary>
    /// Entry point for group editing flow; runs global /start bailout before branching.
    /// </summary>
    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;

        if (!TGBot.StateManager.TryGet(chatId, out IUserState? value) || value is not ProcessContactGroupState userState)
        {
            return;
        }

        switch (userState.currentState)
        {
            case UsersStandardState.ProcessAction:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;

                bool? isUpdateSuccessful = await ProcessUpdate(botClient, update, cancellationToken);
                if (isUpdateSuccessful == true)
                {
                    userState.currentState = UsersStandardState.ProcessData;
                }
                else if (isUpdateSuccessful == null)
                {
                    await CommonUtilities.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetReturnButtonMarkup(),
                        cancellationToken,
                        _resourceService.GetResourceString("InputErrorMessage"));
                }

                break;

            case UsersStandardState.ProcessData:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;
                if (update.CallbackQuery != null && update.CallbackQuery.Data == backCallback)
                {
                    userState.currentState = UsersStandardState.ProcessAction;
                    await CommonUtilities.SendMessage(
                        botClient,
                        update,
                        UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        $"{groupInfo}\n{_resourceService.GetResourceString("ChooseOptionText")}");
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
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetReturnButtonMarkup());
                    return;
                }
                break;

            case UsersStandardState.Finish:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;
                if (update.CallbackQuery != null && update.CallbackQuery.Data == backCallback)
                {
                    userState.currentState = UsersStandardState.ProcessAction;
                    await CommonUtilities.SendMessage(
                        botClient,
                        update,
                        UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        $"{groupInfo}\n{_resourceService.GetResourceString("ChooseOptionText")}");
                    return;
                }
                bool? isMessage = null;
                if (update.Message != null) isMessage = true;

                if (isMessage != true) 
                {
                    callbackAction = update.CallbackQuery!.Data!;
                    ProcessFinish(chatId);
                    string text = !isDBActionSuccessful.Contains(false) ? _resourceService.GetResourceString("SuccessActionResult") : _resourceService.GetResourceString("ErrorActionResult");
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken, text);
        TGBot.StateManager.Remove(chatId);
                    return;
                }
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetReturnButtonMarkup());
                break;
        }
    }

    public async Task<bool?> ProcessUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            callbackAction = update.CallbackQuery!.Data!;
        }

        int userId = _userGetter.GetUserIDbyTelegramID(CommonUtilities.GetIDfromUpdate(update));
        switch (callbackAction)
        {
            case "edit_contact_group":
                if (int.TryParse(update.Message!.Text!, out groupId) && await _groupGetter.GetGroupOwnership(groupId, userId))
                {
                    groupInfo = await UsersGroup.GetUserGroupInfoByGroupId(groupId, _groupGetter);

                    IEnumerable<int> allContactsIds = await _groupGetter.GetAllUsersIdsInGroup(groupId);
                    List<string> allContactsNames = [];

                    foreach (int contactId in allContactsIds)
                    {
                        allContactsNames.Add(_userGetter.GetUserNameByID(contactId) + $" (ID: {contactId})");
                    }

                    string allContactsText;
                    allContactsText = $"{_resourceService.GetResourceString("AllContactsText")} {string.Join("\n", allContactsNames)}";
                    
                    string messageText = $"{groupInfo}\n{allContactsText}\n{_resourceService.GetResourceString("ChooseOptionText")}";
                    await CommonUtilities.SendMessage(
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
                    // Display user's contacts for ID selection
                    int ownerId = _userGetter.GetUserIDbyTelegramID(CommonUtilities.GetIDfromUpdate(update));
                    List<long> tgIds = await _contactGetter.GetAllContactUserTGIds(ownerId);
                    List<string> infos = new();
                    foreach (var tg in tgIds)
                    {
                        int cid = _userGetter.GetUserIDbyTelegramID(tg);
                        string uname = _userGetter.GetUserNameByTelegramID(tg);
                        string link = _userGetter.GetUserSelfLink(tg);
                        infos.Add(string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, link));
                    }
                    string header = _resourceService.GetResourceString("YourContacts");
                    string prompt = _resourceService.GetResourceString("InputContactIDsText");
                    string body = infos.Count > 0 ? string.Join("\n", infos) : _resourceService.GetResourceString("NoUsersFound");
                    await CommonUtilities.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetReturnButton(),
                        cancellationToken,
                        $"{header}\n{body}\n\n{prompt}");
                    return true;
                }
                else if (callbackAction.StartsWith("user_remove_contact_from_group:"))
                {
                    groupId = int.Parse(callbackAction.Split(':')[1]);
                    // Show current members of the group
                    IEnumerable<int> members = await _groupGetter.GetAllUsersIdsInGroup(groupId);
                    List<string> infos = new();
                    foreach (var cid in members)
                    {
                        long tg = _userGetter.GetTelegramIDbyUserID(cid);
                        string uname = _userGetter.GetUserNameByID(cid);
                        string link = _userGetter.GetUserSelfLink(tg);
                        infos.Add(string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, link));
                    }
                    string header = _resourceService.GetResourceString("AllContactsText");
                    string body = infos.Count > 0 ? string.Join("\n", infos) : _resourceService.GetResourceString("NoUsersFound");
                    string prompt = _resourceService.GetResourceString("InputContactIDsText");
                    await botClient.SendMessage(
                        CommonUtilities.GetIDfromUpdate(update),
                        $"{header} {body}\n\n{prompt}",
                        replyMarkup: KeyboardUtils.GetReturnButtonMarkup(),
                        cancellationToken: cancellationToken);
                    return true;
                }
                return null;
        }
    }

    public async Task<bool?> ProcessAction(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        try
        {
                if (callbackAction.StartsWith("user_add_contact_to_group:"))
            {
                groupId = int.Parse(callbackAction.Split(':')[1]);
                contactIDs = update.Message!.Text!.Split(" ").Select(x => int.Parse(x)).ToList();
                List<int> allowedIds = [];
                foreach (int contactId in contactIDs)
                {
                    bool status = _contactGroupRepository.CheckUserAndContactConnect(userId, contactId);
                    if (status) allowedIds.Add(contactId);
                }

                if (allowedIds.Count == 0) return null;
                // Show confirmation summary with contact short data
                List<string> confirmInfos = new();
                foreach (var cid in allowedIds)
                {
                    long tg = _userGetter.GetTelegramIDbyUserID(cid);
                    string uname = _userGetter.GetUserNameByID(cid);
                    string link = _userGetter.GetUserSelfLink(tg);
                    confirmInfos.Add(string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, link));
                }
                string confirmHeader = _resourceService.GetResourceString("ConfirmAddContactsToGroupText");
                await CommonUtilities.SendMessage(
                    botClient,
                    update,
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_add_contact_to_group"),
                    cancellationToken,
                    $"{confirmHeader}\n\n{string.Join("\n", confirmInfos)}");
                contactIDs = allowedIds;
                return true;
            }
            else if (callbackAction.StartsWith("user_remove_contact_from_group:"))
            {
                groupId = int.Parse(callbackAction.Split(':')[1]);
                contactIDs = update.Message!.Text!.Split(" ").Select(x => int.Parse(x)).ToList();
                // Confirmation summary for removal
                List<string> confirmInfos = new();
                foreach (var cid in contactIDs)
                {
                    long tg = _userGetter.GetTelegramIDbyUserID(cid);
                    string uname = _userGetter.GetUserNameByID(cid);
                    string link = _userGetter.GetUserSelfLink(tg);
                    confirmInfos.Add(string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, link));
                }
                string confirmHeader = _resourceService.GetResourceString("ConfirmDeleteContactsFromGroupText");
                await CommonUtilities.SendMessage(
                    botClient,
                    update,
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_delete_contact_from_group"),
                    cancellationToken,
                    $"{confirmHeader}\n\n{string.Join("\n", confirmInfos)}");
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
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        if (callbackAction.StartsWith("accept_add_contact_to_group"))
        {
            foreach (var contactId in contactIDs)
            {
                isDBActionSuccessful.Add(_contactGroupRepository.AddContactToGroup(userId, contactId, groupId));
            }
        }
        else if (callbackAction.StartsWith("accept_delete_contact_from_group"))
        {
            foreach (var contactId in contactIDs)
            {
                isDBActionSuccessful.Add(_contactGroupRepository.RemoveContactFromGroup(userId, contactId, groupId));
            }
        }
    }
}