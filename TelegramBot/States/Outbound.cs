// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;


namespace TelegramMediaRelayBot;

/// <summary>
/// Manages outbound invites: view, revoke, and details. Follows ProcessAction -> Finish.
/// Adds global /start bailout at the beginning of processing.
/// </summary>
public class UserProcessOutboundState : IUserState
{
    private IContactRemover _contactRepository;
    private IOutboundDBGetter _outboundDBGetter;
    private IUserGetter _userGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;
    public UserOutboundState currentState;

    public UserProcessOutboundState(
        IContactRemover contactRepository,
        IOutboundDBGetter outboundDBGetter,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        currentState = UserOutboundState.ProcessAction;
        _contactRepository = contactRepository;
        _outboundDBGetter = outboundDBGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public static UserOutboundState[] GetAllStates()
    {
        return (UserOutboundState[])Enum.GetValues(typeof(UserOutboundState));
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    /// <summary>
    /// Entry point for processing outbound state updates. Applies /start bailout first.
    /// </summary>
    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;

        if (!TGBot.StateManager.TryGet(chatId, out IUserState? value) || value is not UserProcessOutboundState userState)
        {
            return;
        }

        switch (userState.currentState)
        {
            case UserOutboundState.ProcessAction:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("revoke_outbound_invite:"))
                {
                    string userId = update.CallbackQuery.Data.Split(':')[1];
                    await CommonUtilities.SendMessage(botClient, update, OutBoundKB.GetOutBoundActionsKeyboardMarkup(userId, "user_show_outbound_invite:" + chatId),
                                                cancellationToken, _resourceService.GetResourceString("DeclineOutBound"));
                    userState.currentState = UserOutboundState.Finish;
                    return;
                }
        TGBot.StateManager.Remove(chatId);
                await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update, _outboundDBGetter, _userGetter);
                break;

            case UserOutboundState.Finish:
                if (update.CallbackQuery != null)
                {
                    if (update.CallbackQuery.Data!.StartsWith("user_show_outbound_invite:"))
                    {
        TGBot.StateManager.Remove(chatId);
                        await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId, _contactRepository, _outboundDBGetter, _userGetter, _resourceService);
                        return;
                    }
                    else if (update.CallbackQuery.Data!.StartsWith("user_accept_revoke_outbound_invite:"))
                    {
                        string userId = update.CallbackQuery.Data.Split(':')[1];
                        int accepterTelegramID = _userGetter.GetUserIDbyTelegramID(long.Parse(userId));
                        await _contactRepository.RemoveContactByStatus(_userGetter.GetUserIDbyTelegramID(chatId), accepterTelegramID, ContactsStatus.WAITING_FOR_ACCEPT);
                    }
                }
        TGBot.StateManager.Remove(chatId);
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
        }
    }
}