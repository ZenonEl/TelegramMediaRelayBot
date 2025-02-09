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
using TelegramMediaRelayBot.TelegramBot.Utils ;
using TelegramMediaRelayBot;


namespace MediaTelegramBot;

public class UserProcessInboundState : IUserState
{
    public UserInboundState currentState;

    public UserProcessInboundState()
    {
        currentState = UserInboundState.SelectInvite;
    }

    public static UserInboundState[] GetAllStates()
    {
        return (UserInboundState[])Enum.GetValues(typeof(UserInboundState));
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (!TelegramBot.userStates.TryGetValue(chatId, out IUserState? value))
        {
            return;
        }

        var userState = (UserProcessInboundState)value;

        switch (userState.currentState)
        {
            case UserInboundState.SelectInvite:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("user_show_inbounds_invite:"))
                {
                    string userId = update.CallbackQuery.Data.Split(':')[1];
                    await CommonUtilities.SendMessage(botClient, update, InBoundKB.GetInBoundActionsKeyboardMarkup(userId, "view_inbound_invite_links"),
                                                cancellationToken, Config.GetResourceString("SelectAction"));
                    userState.currentState = UserInboundState.ProcessAction;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;

            case UserInboundState.ProcessAction:
                if (update.Message != null && update.Message.Text != null)
                {
                    TelegramBot.userStates.Remove(chatId);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    return;
                }
                else if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("view_inbound_invite_links"))
                {
                    await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, chatId);
                    return;
                }
                string userID = update.CallbackQuery!.Data!.Split(':')[1];
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("user_accept_inbounds_invite:"))
                {
                    await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetConfirmForActionKeyboardMarkup($"accept_accept_invite:{userID}", $"decline_accept_invite:{userID}"),
                    cancellationToken, Config.GetResourceString("WaitAcceptInboundInvite"));
                }
                else if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("user_decline_inbounds_invite:"))
                {
                    await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetConfirmForActionKeyboardMarkup($"accept_decline_invite:{userID}", $"decline_decline_invite:{userID}"), 
                    cancellationToken, Config.GetResourceString("WaitDeclineInboundInvite"));
                }

                userState.currentState = UserInboundState.Finish;
                break;

            case UserInboundState.Finish:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("accept_accept_invite:"))
                {
                    await CallbackQueryMenuUtils.AcceptInboundInvite(update);
                }
                else if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("accept_decline_invite:"))
                {
                    await CallbackQueryMenuUtils.DeclineInboundInvite(update);
                }
                else if (update.CallbackQuery != null && !update.CallbackQuery.Data!.StartsWith("main_menu"))
                {
                    string userId = update.CallbackQuery.Data.Split(':')[1];
                    await CommonUtilities.SendMessage(botClient, update, InBoundKB.GetInBoundActionsKeyboardMarkup(userId, "view_inbound_invite_links"),
                                                cancellationToken, Config.GetResourceString("SelectAction"));
                    userState.currentState = UserInboundState.ProcessAction;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
        }
    }
}