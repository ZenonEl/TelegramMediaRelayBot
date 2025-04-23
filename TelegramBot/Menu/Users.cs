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
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.Menu;

public class Users
{
    public static CancellationToken cancellationToken = TGBot.cancellationToken;

    public static async Task ViewSettings(ITelegramBotClient botClient, Update update)
    {
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
            UsersDefaultActionsMenuKB.GetDefaultActionsMenuKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("DefaultActionsMenuText")
        );
    }

    public static async Task ViewPrivacyMenu(ITelegramBotClient botClient, Update update, string statusMessage = "")
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetPrivacyMenuKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("ChosePrivacyOptionMenuText") + "\n\n" + statusMessage
        );
    }

    public static async Task ViewLinkPrivacyMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("SelfLinkRefreshMenuText")
        );
    }

    public static async Task ViewPermanentContentSpoilerMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetPermanentContentSpoilerKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("AllowForwardContentRuleText")
        );
    }

    public static async Task ProcessViewPermanentContentSpoilerAction(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetPermanentContentSpoilerKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("AllowForwardContentRuleText")
        );
    }

    public static async Task ViewVideoDefaultActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersDefaultActionsMenuKB.GetDefaultVideoDistributionKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("VideoDefaultActionsMenuText")
        );
    }

    public static async Task ViewUsersVideoSentUsersActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("UsersVideoSentUsersMenuText")
        );
    }

    public static async Task ViewAutoSendVideoTimeMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersDefaultActionsMenuKB.GetUsersAutoSendVideoTimeKeyboardMarkup(),
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

public class UsersDB
{

    public static void UpdateSelfLinkWithKeepSelectedContacts(Update update)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        TGBot.userStates[chatId] = new ProcessContactLinksState(false);
    }

    public static void UpdateSelfLinkWithDeleteSelectedContacts(Update update)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        TGBot.userStates[chatId] = new ProcessContactLinksState(true);
    }

    public static bool UpdateSelfLinkWithContacts(Update update)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        return CoreDB.ReCreateSelfLink(DBforGetters.GetUserIDbyTelegramID(chatId));
    }

    public static void UpdateSelfLinkWithNewContacts(Update update)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(CommonUtilities.GetIDfromUpdate(update));
        CoreDB.ReCreateSelfLink(userId);
        ContactRemover.RemoveAllContacts(userId);
    }
}