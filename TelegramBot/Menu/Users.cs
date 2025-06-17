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

    public static async Task ViewWhoCanFindMeByLinkMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetWhoCanFindMeByLinkKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("SearchPrivacyText")
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

    public static bool SetAutoSendVideoTimeToUser(long chatId, string time, IDefaultActionSetter defaultActionSetter, IUserGetter userGetter)
    {
        int userId = userGetter.GetUserIDbyTelegramID(chatId);
        return defaultActionSetter.SetAutoSendVideoConditionToUser(userId, time, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }

    public static bool SetDefaultActionToUser(long chatId, string action, IDefaultActionSetter defaultActionSetter, IUserGetter userGetter)
    {
        int userId = userGetter.GetUserIDbyTelegramID(chatId);
        return defaultActionSetter.SetAutoSendVideoActionToUser(userId, action, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }

    public static async Task ViewSiteFilterMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetSiteFilterKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("SiteFilterMenuText")
        );
    }

    public static async Task ViewSiteFilterSettingsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetSiteFilterSettingsKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("SiteFilterMenuText")
        );
    }
}

public class UsersDB
{
    public static void UpdateSelfLinkWithKeepSelectedContacts(
        Update update,
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        TGBot.userStates[chatId] = new ProcessContactLinksState(false, contactRemoverRepository, contactGetterRepository, userRepository, userGetter);
    }

    public static void UpdateSelfLinkWithDeleteSelectedContacts(Update update,
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        TGBot.userStates[chatId] = new ProcessContactLinksState(true, contactRemoverRepository, contactGetterRepository, userRepository, userGetter);
    }

    public static bool UpdateSelfLinkWithContacts(Update update, IUserRepository userRepository, IUserGetter userGetter)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        return userRepository.ReCreateUserSelfLink(userGetter.GetUserIDbyTelegramID(chatId));
    }

    public static void UpdateSelfLinkWithNewContacts(Update update, IContactRemover contactRemover, IUserRepository userRepository, IUserGetter userGetter)
    {
        int userId = userGetter.GetUserIDbyTelegramID(CommonUtilities.GetIDfromUpdate(update));
        userRepository.ReCreateUserSelfLink(userId);
        contactRemover.RemoveAllContacts(userId);
    }
}