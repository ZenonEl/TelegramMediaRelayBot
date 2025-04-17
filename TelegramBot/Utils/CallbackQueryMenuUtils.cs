// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot.Types;
using MediaTelegramBot;


namespace TelegramMediaRelayBot.TelegramBot.Utils ;

public static class CallbackQueryMenuUtils
{
    public static CancellationToken cancellationToken = MediaTelegramBot.TelegramBot.cancellationToken;

    public static async Task GetSelfLink(ITelegramBotClient botClient, Update update)
    {
        string link = DBforGetters.GetSelfLink(update.CallbackQuery!.Message!.Chat.Id);
        User me = await botClient.GetMe();
        string startLink = $"\nhttps://t.me/{me.Username}?start={link}";
        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(Config.GetResourceString("YourLink") + startLink, link));
    }

    public static async Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update, long chatId)
    {
        string text = Config.GetResourceString("YourInboundInvitations");
        await CommonUtilities.SendMessage(botClient, update, InBoundKB.GetInboundsKeyboardMarkup(update), cancellationToken, text);
        MediaTelegramBot.TelegramBot.userStates[chatId] = new UserProcessInboundState();
    }

    public static async Task ViewOutboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        string text = Config.GetResourceString("YourOutboundInvitations");
        await CommonUtilities.SendMessage(botClient, update, OutBoundKB.GetOutboundKeyboardMarkup(CommonUtilities.GetIDfromUpdate(update)), cancellationToken, text);
    }

    public static async Task ShowOutboundInvite(ITelegramBotClient botClient, Update update, long chatId)
    {
        string userId = update.CallbackQuery!.Data!.Split(':')[1];
        await CommonUtilities.SendMessage(botClient, update, OutBoundKB.GetOutboundActionsKeyboardMarkup(userId), cancellationToken, Config.GetResourceString("OutboundInviteMenu"));
        MediaTelegramBot.TelegramBot.userStates[chatId] = new UserProcessOutboundState();
    }

    public static Task AcceptInboundInvite(Update update)
    {
        ContactSetter.SetContactStatus(long.Parse(update.CallbackQuery!.Data!.Split(':')[1]), update.CallbackQuery.Message!.Chat.Id, DataBase.Types.ContactsStatus.ACCEPTED);
        return Task.CompletedTask;
    }

    public static Task DeclineInboundInvite(Update update)
    {
        string userId = update.CallbackQuery!.Data!.Split(':')[1];
        int senderTelegramID = DBforGetters.GetUserIDbyTelegramID(long.Parse(userId));
        int accepterTelegramID = DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery.Message!.Chat.Id);
        ContactRemover.RemoveContactByStatus(senderTelegramID, accepterTelegramID, DataBase.Types.ContactsStatus.WAITING_FOR_ACCEPT);
        return Task.CompletedTask;
    }

    public static Task WhosTheGenius(ITelegramBotClient botClient, Update update)
    {
        string text = Config.GetResourceString("WhosTheGeniusText");
        return CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, text);
    }
}