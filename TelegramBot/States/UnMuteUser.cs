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
using TelegramMediaRelayBot.TelegramBot.Utils;


namespace TelegramMediaRelayBot;

/// <summary>
/// Unmutes a previously muted contact for the current user. Unified flow:
/// WaitingForLinkOrID -> WaitingForUnMute -> Finish. Supports /start bailout.
/// </summary>
public class ProcessUserUnMuteState : IUserState
{
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;
    public UserUnMuteState currentState;

    private int mutedByUserId { get; set; }
    private int mutedContactId { get; set; }
    private readonly IContactRemover _contactRemover;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;

    public ProcessUserUnMuteState(
        IContactRemover contactRemover,
        IContactGetter contactGetters,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService
        )
    {
        currentState = UserUnMuteState.WaitingForLinkOrID;
        _contactRemover = contactRemover;
        _contactGetter = contactGetters;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public static UserUnMuteState[] GetAllStates()
    {
        return (UserUnMuteState[])Enum.GetValues(typeof(UserUnMuteState));
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    /// <summary>
    /// Processes state transitions and handles global /start bailout before branching.
    /// </summary>
    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId)) return;

        if (!TGBot.StateManager.TryGet(chatId, out IUserState? value))
        {
            return;
        }

        var userState = (ProcessUserUnMuteState)value;

        switch (userState.currentState)
        {
            case UserUnMuteState.WaitingForLinkOrID:
                var message = update.Message;
                if (message == null || string.IsNullOrWhiteSpace(message.Text))
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("InvalidInputValues"));
                    return;
                }
                int contactId;
                if (int.TryParse(message.Text, out contactId))
                {
                    List<long> allowedIds = await _contactGetter.GetAllContactUserTGIds(_userGetter.GetUserIDbyTelegramID(message.Chat.Id));
                    string name = _userGetter.GetUserNameByID(contactId);

                    if (name == "" || !allowedIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                    {
                        await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("NoUserFoundByID"));
                        return;
                    }
                    await botClient.SendMessage(chatId, string.Format(_resourceService.GetResourceString("WillWorkWithContact"), contactId, name), cancellationToken: cancellationToken);
                }
                else
                {
                    string link = message.Text!;
                    contactId = await _contactGetter.GetContactIDByLinkAsync(link);
                    List<long> allowedIds = await _contactGetter.GetAllContactUserTGIds(_userGetter.GetUserIDbyTelegramID(message.Chat.Id));

                    if (contactId == -1 || !allowedIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                    {
                        await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("NoUserFoundByLink"));
                        return;
                    }
                    string name = _userGetter.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, string.Format(_resourceService.GetResourceString("WillWorkWithContact"), contactId, name), cancellationToken: cancellationToken);
                }
                mutedByUserId = _userGetter.GetUserIDbyTelegramID(chatId);
                mutedContactId = contactId;
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("ConfirmDecision"), cancellationToken: cancellationToken);
                userState.currentState = UserUnMuteState.WaitingForUnMute;
                break;

            case UserUnMuteState.WaitingForUnMute:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId)) return;

                string activeMuteTime = await _contactGetter.GetActiveMuteTimeByContactIDAsync(mutedContactId);
                string text = string.Format(_resourceService.GetResourceString("UserInMute"), activeMuteTime);
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
                userState.currentState = UserUnMuteState.Finish;
                break;

            case UserUnMuteState.Finish:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId)) return;
                await _contactRemover.RemoveMutedContact(mutedByUserId, mutedContactId);
                await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("UserUnmuted"));
        TGBot.StateManager.Remove(chatId);
                break;
        }
    }
}