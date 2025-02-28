// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;


namespace MediaTelegramBot;

public class UserProcessOutboundState : IUserState
{
    public UserOutboundState currentState;

    public UserProcessOutboundState()
    {
        currentState = UserOutboundState.ProcessAction;
    }

    public static UserOutboundState[] GetAllStates()
    {
        return (UserOutboundState[])Enum.GetValues(typeof(UserOutboundState));
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

        var userState = (UserProcessOutboundState)value;

        switch (userState.currentState)
        {
            case UserOutboundState.ProcessAction:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("revoke_outbound_invite:"))
                {
                    string userId = update.CallbackQuery.Data.Split(':')[1];
                    await Utils.Utils.SendMessage(botClient, update, OutBoundKB.GetOutBoundActionsKeyboardMarkup(userId, "user_show_outbound_invite:" + chatId),
                                                cancellationToken, Config.GetResourceString("DeclineOutBound"));
                    userState.currentState = UserOutboundState.Finish;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update);
                break;

            case UserOutboundState.Finish:
                if (update.CallbackQuery != null)
                {
                    if (update.CallbackQuery.Data!.StartsWith("user_show_outbound_invite:"))
                    {
                        TelegramBot.userStates.Remove(chatId);
                        await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId);
                        return;
                    }
                    else if (update.CallbackQuery.Data!.StartsWith("user_accept_revoke_outbound_invite:"))
                    {
                        string userId = update.CallbackQuery.Data.Split(':')[1];
                        int accepterTelegramID = DBforGetters.GetUserIDbyTelegramID(long.Parse(userId));
                        CoreDB.RemoveContact(DBforGetters.GetUserIDbyTelegramID(chatId), accepterTelegramID);
                    }
                }
                TelegramBot.userStates.Remove(chatId);
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
        }
    }
}