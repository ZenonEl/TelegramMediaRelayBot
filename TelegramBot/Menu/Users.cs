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
            Localization.Get("SettingsMenuText")
        );
    }

    public static async Task ViewDefaultActionsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersDefaultActionsMenuKB.GetDefaultActionsMenuKeyboardMarkup(),
            cancellationToken,
            Localization.Get("DefaultActionsMenuText")
        );
    }

    public static async Task ViewPrivacyMenu(ITelegramBotClient botClient, Update update, string statusMessage = "")
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetPrivacyMenuKeyboardMarkup(),
            cancellationToken,
            Localization.Get("ChosePrivacyOptionMenuText") + "\n\n" + statusMessage
        );
    }

    public static async Task ViewLinkPrivacyMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
            cancellationToken,
            Localization.Get("SelfLinkRefreshMenuText")
        );
    }

    public static async Task ViewWhoCanFindMeByLinkMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetWhoCanFindMeByLinkKeyboardMarkup(),
            cancellationToken,
            Localization.Get("SearchPrivacyText")
        );
    }

    public static async Task ViewPermanentContentSpoilerMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetPermanentContentSpoilerKeyboardMarkup(),
            cancellationToken,
            Localization.Get("AllowForwardContentRuleText")
        );
    }

    public static async Task ProcessViewPermanentContentSpoilerAction(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetPermanentContentSpoilerKeyboardMarkup(),
            cancellationToken,
            Localization.Get("AllowForwardContentRuleText")
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

        string message = Localization.Get("VideoDefaultActionsMenuText");
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
                statusText = $"{activeIndicator} {Localization.Get("SettingsActionLabel")}: {actionName}";
            }
        }

        string message = Localization.Get("UsersVideoSentUsersMenuText");
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
                statusText = $"{activeIndicator} {Localization.Get("SettingsDelayLabel")}: {settings.ActionCondition} {Localization.Get("SettingsSecondsLabel")}";
            }
        }

        string message = Localization.Get("AutoSendVideoTimeMenuText");
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
            return $"📋 {Localization.Get("SettingsCurrentStateText")} {Localization.Get("SettingsNotConfigured")}";

        var sb = new StringBuilder();
        sb.AppendLine($"📋 {Localization.Get("SettingsCurrentStateText")}");

        string statusIndicator = settings.IsActive ? "✅" : "❌";
        string statusText = settings.IsActive
            ? Localization.Get("SettingsEnabled")
            : Localization.Get("SettingsDisabled");
        sb.AppendLine($"  {Localization.Get("SettingsStatusLabel")}: {statusIndicator} {statusText}");

        if (!string.IsNullOrEmpty(settings.Action))
        {
            string actionName = GetActionDisplayName(settings.Action);
            sb.AppendLine($"  {Localization.Get("SettingsActionLabel")}: {actionName}");
        }

        if (!string.IsNullOrEmpty(settings.ActionCondition))
        {
            sb.AppendLine($"  {Localization.Get("SettingsDelayLabel")}: {settings.ActionCondition} {Localization.Get("SettingsSecondsLabel")}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string GetActionDisplayName(string action)
    {
        return action switch
        {
            UsersAction.SEND_MEDIA_TO_ALL_CONTACTS => Localization.Get("AllContactsButtonText"),
            UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS => Localization.Get("DefaultGroupsButtonText"),
            UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS => Localization.Get("SpecifiedContactsButtonText"),
            UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS => Localization.Get("SpecifiedGroupsButtonText"),
            UsersAction.SEND_MEDIA_ONLY_TO_ME => Localization.Get("OnlyMeButtonText"),
            UsersAction.OFF => Localization.Get("DisableAutoSendButtonText"),
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
            Localization.Get("SiteFilterMenuText")
        );
    }

    public static async Task ViewSiteFilterSettingsMenu(ITelegramBotClient botClient, Update update)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetSiteFilterSettingsKeyboardMarkup(),
            cancellationToken,
            Localization.Get("SiteFilterMenuText")
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