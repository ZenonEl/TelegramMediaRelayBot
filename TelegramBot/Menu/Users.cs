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
using MediaTelegramBot.Utils;
using DataBase;
using TelegramMediaRelayBot;
using DataBase.Types;

namespace MediaTelegramBot.Menu;

public class Users
{
    public static CancellationToken cancellationToken = TelegramBot.cancellationToken;

    public static async Task ViewSettings(ITelegramBotClient botClient, Update update)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);

        await Utils.Utils.SendMessage(
            botClient,
            update,
            UsersKB.GetSettingsKeyboardMarkup(),
            cancellationToken,
            "Доступные настройки:"
        );
    }

    public static async Task ViewDefaultActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await Utils.Utils.SendMessage(
            botClient,
            update,
            UsersKB.GetDefaultActionsMenuKeyboardMarkup(),
            cancellationToken,
            "Доступные действия по умолчанию:"
        );
    }

    public static async Task ViewVideoDefaultActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await Utils.Utils.SendMessage(
            botClient,
            update,
            UsersKB.GetDefaultVideoDistributionKeyboardMarkup(),
            cancellationToken,
            "Доступные действия для настройки действий по умолчанию при обработке ссылок:"
        );
    }

    public static async Task ViewUsersVideoSentUsersActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await Utils.Utils.SendMessage(
            botClient,
            update,
            UsersKB.GetUsersVideoSentUsersKeyboardMarkup(),
            cancellationToken,
            "Доступные действия для настройки рассылки по умолчанию:"
        );
    }

    public static async Task ViewAutoSendVideoTimeMenu(ITelegramBotClient botClient, Update update)
    {
        await Utils.Utils.SendMessage(
            botClient,
            update,
            UsersKB.GetUsersAutoSendVideoTimeKeyboardMarkup(),
            cancellationToken,
            "Доступные действия для настройки рассылки по умолчанию:"
        );
    }

    public static bool SetAutoSendVideoTimeToUser(long chatId, string time)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        return CoreDB.SetAutoSendVideoConditionToUser(userId, time, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }

    public static bool SetDefaultActionToUser(long chatId, string action)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        return CoreDB.SetAutoSendVideoActionToUser(userId, action, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }
}