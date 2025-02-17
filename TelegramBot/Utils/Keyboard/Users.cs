// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using DataBase.Types;
using Telegram.Bot.Types.ReplyMarkups;


namespace TelegramMediaRelayBot.TelegramBot.Utils;

public static class UsersKB
{

    public static InlineKeyboardMarkup GetSettingsKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DefaultActionsButtonText"), $"default_actions_menu"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("PrivacyButtonText"), $"privacy_menu"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton()
            },
        });
        return inlineKeyboard;
    }
}

public static class UsersPrivacyMenuKB
{
    public static InlineKeyboardMarkup GetPrivacyMenuKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("SelfLinkRefreshButtonText"), $"user_update_self_link"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("show_settings")
            },
        });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetUpdateSelfLinkKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("SelfLinkRefreshButtonTextOption1"), $"user_update_self_link_with_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("SelfLinkRefreshButtonTextOption2"), $"user_update_self_link_with_new_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("SelfLinkRefreshButtonTextOption3"), $"user_update_self_link"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("privacy_menu")
            },
        });
        return inlineKeyboard;
    }
}

public static class UsersDefaultActionsMenuKB
{
    public static InlineKeyboardMarkup GetDefaultActionsMenuKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DefaultVideoActionsButtonText"), $"video_default_actions_menu"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("show_settings")
            },
        });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetDefaultVideoDistributionKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("VideoRecipientsButtonText"), $"user_set_video_send_users"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("AutoSendTimeButtonText"), $"user_set_auto_send_video_time"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("default_actions_menu")
            },
        });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetUsersVideoSentUsersKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("AllContactsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_ALL_CONTACTS}"),
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DefaultGroupsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("SpecifiedContactsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS}"),
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("SpecifiedGroupsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("OnlyMeButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_ONLY_TO_ME}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DisableAutoSendButtonText"), $"user_set_video_send_users:{UsersAction.OFF}"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("video_default_actions_menu")
            },
        });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetUsersAutoSendVideoTimeKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("FiveSecondsButtonText"), $"user_set_auto_send_video_time_to:5"),
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("TenSecondsButtonText"), $"user_set_auto_send_video_time_to:10"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ThirtySecondsButtonText"), $"user_set_auto_send_video_time_to:30"),
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("OneMinuteButtonText"), $"user_set_auto_send_video_time_to:60"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("TenMinutesButtonText"), $"user_set_auto_send_video_time_to:600"),
                InlineKeyboardButton.WithCallbackData(Config.GetResourceString("SixtyMinutesButtonText"), $"user_set_auto_send_video_time_to:3600"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("video_default_actions_menu")
            },
        });
        return inlineKeyboard;
    }
}