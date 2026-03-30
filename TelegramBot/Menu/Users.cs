// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using System.Text;
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

    public static async Task ViewVideoDefaultActionsMenu(ITelegramBotClient botClient, Update update,
        IDefaultActionGetter? defaultActionGetter = null, IUserGetter? userGetter = null)
    {
        string statusText = "";
        if (defaultActionGetter != null && userGetter != null)
        {
            long chatId = CommonUtilities.GetIDfromUpdate(update);
            int userId = userGetter.GetUserIDbyTelegramID(chatId);
            statusText = FormatSettingsStatus(defaultActionGetter, userId);
        }

        string message = Config.GetResourceString("VideoDefaultActionsMenuText");
        if (!string.IsNullOrEmpty(statusText))
            message += "\n\n" + statusText;

        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersDefaultActionsMenuKB.GetDefaultVideoDistributionKeyboardMarkup(),
            cancellationToken,
            message
        );
    }

    public static async Task ViewUsersVideoSentUsersActionsMenu(ITelegramBotClient botClient, Update update,
        IDefaultActionGetter? defaultActionGetter = null, IUserGetter? userGetter = null)
    {
        string statusText = "";
        if (defaultActionGetter != null && userGetter != null)
        {
            long chatId = CommonUtilities.GetIDfromUpdate(update);
            int userId = userGetter.GetUserIDbyTelegramID(chatId);
            var settings = defaultActionGetter.GetDefaultActionSettings(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            if (!string.IsNullOrEmpty(settings.Action))
            {
                string actionName = GetActionDisplayName(settings.Action);
                string activeIndicator = settings.IsActive ? "✅" : "❌";
                statusText = $"{activeIndicator} {Config.GetResourceString("SettingsActionLabel")}: {actionName}";
            }
        }

        string message = Config.GetResourceString("UsersVideoSentUsersMenuText");
        if (!string.IsNullOrEmpty(statusText))
            message += "\n\n" + statusText;

        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
            cancellationToken,
            message
        );
    }

    public static async Task ViewAutoSendVideoTimeMenu(ITelegramBotClient botClient, Update update,
        IDefaultActionGetter? defaultActionGetter = null, IUserGetter? userGetter = null)
    {
        string statusText = "";
        if (defaultActionGetter != null && userGetter != null)
        {
            long chatId = CommonUtilities.GetIDfromUpdate(update);
            int userId = userGetter.GetUserIDbyTelegramID(chatId);
            var settings = defaultActionGetter.GetDefaultActionSettings(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            if (!string.IsNullOrEmpty(settings.ActionCondition))
            {
                string activeIndicator = settings.IsActive ? "✅" : "❌";
                statusText = $"{activeIndicator} {Config.GetResourceString("SettingsDelayLabel")}: {settings.ActionCondition} {Config.GetResourceString("SettingsSecondsLabel")}";
            }
        }

        string message = Config.GetResourceString("AutoSendVideoTimeMenuText");
        if (!string.IsNullOrEmpty(statusText))
            message += "\n\n" + statusText;

        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersDefaultActionsMenuKB.GetUsersAutoSendVideoTimeKeyboardMarkup(),
            cancellationToken,
            message
        );
    }

    public static string FormatSettingsStatus(IDefaultActionGetter defaultActionGetter, int userId)
    {
        var settings = defaultActionGetter.GetDefaultActionSettings(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);

        if (string.IsNullOrEmpty(settings.Action) && string.IsNullOrEmpty(settings.ActionCondition))
            return $"📋 {Config.GetResourceString("SettingsCurrentStateText")} {Config.GetResourceString("SettingsNotConfigured")}";

        var sb = new StringBuilder();
        sb.AppendLine($"📋 {Config.GetResourceString("SettingsCurrentStateText")}");

        string statusIndicator = settings.IsActive ? "✅" : "❌";
        string statusText = settings.IsActive
            ? Config.GetResourceString("SettingsEnabled")
            : Config.GetResourceString("SettingsDisabled");
        sb.AppendLine($"  {Config.GetResourceString("SettingsStatusLabel")}: {statusIndicator} {statusText}");

        if (!string.IsNullOrEmpty(settings.Action))
        {
            string actionName = GetActionDisplayName(settings.Action);
            sb.AppendLine($"  {Config.GetResourceString("SettingsActionLabel")}: {actionName}");
        }

        if (!string.IsNullOrEmpty(settings.ActionCondition))
        {
            sb.AppendLine($"  {Config.GetResourceString("SettingsDelayLabel")}: {settings.ActionCondition} {Config.GetResourceString("SettingsSecondsLabel")}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string GetActionDisplayName(string action)
    {
        return action switch
        {
            UsersAction.SEND_MEDIA_TO_ALL_CONTACTS => Config.GetResourceString("AllContactsButtonText"),
            UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS => Config.GetResourceString("DefaultGroupsButtonText"),
            UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS => Config.GetResourceString("SpecifiedContactsButtonText"),
            UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS => Config.GetResourceString("SpecifiedGroupsButtonText"),
            UsersAction.SEND_MEDIA_ONLY_TO_ME => Config.GetResourceString("OnlyMeButtonText"),
            UsersAction.OFF => Config.GetResourceString("DisableAutoSendButtonText"),
            _ => action
        };
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
        UserSessionManager.Set(chatId, new ProcessContactLinksState(false, contactRemoverRepository, contactGetterRepository, userRepository, userGetter));
    }

    public static void UpdateSelfLinkWithDeleteSelectedContacts(Update update,
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        UserSessionManager.Set(chatId, new ProcessContactLinksState(true, contactRemoverRepository, contactGetterRepository, userRepository, userGetter));
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