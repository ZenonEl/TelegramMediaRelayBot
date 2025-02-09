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
using DataBase;
using DataBase.Types;
using TelegramMediaRelayBot;

namespace MediaTelegramBot.Menu;

public class Users
{
    public static CancellationToken cancellationToken = TelegramBot.cancellationToken;

    public static async Task ViewSettings(ITelegramBotClient botClient, Update update)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);

        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersKB.GetSettingsKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("SettingsMenuText")
        );
    }

    public static async Task ViewDefaultActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersKB.GetDefaultActionsMenuKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("DefaultActionsMenuText")
        );
    }

    public static async Task ViewVideoDefaultActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersKB.GetDefaultVideoDistributionKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("VideoDefaultActionsMenuText")
        );
    }

    public static async Task ViewUsersVideoSentUsersActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersKB.GetUsersVideoSentUsersKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("UsersVideoSentUsersMenuText")
        );
    }

    public static async Task ViewAutoSendVideoTimeMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersKB.GetUsersAutoSendVideoTimeKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("AutoSendVideoTimeMenuText")
        );
    }

    public static bool SetAutoSendVideoTimeToUser(long chatId, string time)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        return DBforDefaultActions.SetAutoSendVideoConditionToUser(userId, time, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }

    public static bool SetDefaultActionToUser(long chatId, string action)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        return DBforDefaultActions.SetAutoSendVideoActionToUser(userId, action, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }
}