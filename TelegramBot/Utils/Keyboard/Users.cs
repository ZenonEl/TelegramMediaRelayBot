// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database;


namespace TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

public static class UsersKB
{
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }

    public static InlineKeyboardMarkup GetSettingsKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("DefaultActionsButtonText"), "default_actions_menu"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("PrivacyButtonText"), "privacy_menu_and_safety"),
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
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }

    public static InlineKeyboardMarkup GetPrivacyMenuKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("SelfLinkRefreshButtonText"), "user_update_self_link"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("WhoCanFindMeByLink"), "user_update_who_can_find_me_by_link"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("SiteStopList"), "user_update_site_stop_list"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("AllowContentForwardingBtn"), "user_update_content_forwarding_rule"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("show_settings")
            },
        });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetWhoCanFindMeByLinkKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Никто", "user_set_nobody_who_can_find_me_by_link"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Только общие контакты", "user_set_general_who_can_find_me_by_link"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Все", "user_set_all_who_can_find_me_by_link"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("privacy_menu_and_safety")
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
                InlineKeyboardButton.WithCallbackData(GetResourceString("SelfLinkRefreshButtonTextOption1"), "user_update_self_link_with_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("SelfLinkRefreshButtonTextOption2"), "user_update_self_link_with_new_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("SelfLinkRefreshButtonTextOption3"), "user_update_self_link_with_keep_selected_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("SelfLinkRefreshButtonTextOption4"), "user_update_self_link_with_delete_selected_contacts"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("privacy_menu_and_safety")
            },
        });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetPermanentContentSpoilerKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("Enable"), "user_allow_content_forwarding"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("Disable"), "user_disallow_content_forwarding"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("privacy_menu_and_safety")
            },
        });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetSiteFilterKeyboardMarkup()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("SocialFilterButton"), 
                    "user_set_site_stop_list:social"
                ), 
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("NSFWFilterButton"),
                    "user_set_site_stop_list:nsfw"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("UnifiedFilterButton"),
                    "user_set_site_stop_list:unified"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("DomainFilterButton"),
                    "user_set_site_stop_list:domains"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("ConfigureDomainsButton"),
                    "user_set_site_stop_list_settings"
                ),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("privacy_menu_and_safety")
            },
        });
    }

    public static InlineKeyboardMarkup GetSiteFilterSettingsKeyboardMarkup()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("AddDomainButton"),
                    "user_set_site_stop_list:add_domains"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("RemoveDomainButton"),
                    "user_set_site_stop_list:remove_domains"
                ),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("user_update_site_stop_list")
            },
        });
    }
}

public static class UsersDefaultActionsMenuKB
{
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }


    public static InlineKeyboardMarkup GetDefaultActionsMenuKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("DefaultVideoActionsButtonText"), "video_default_actions_menu"),
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
                InlineKeyboardButton.WithCallbackData(GetResourceString("VideoRecipientsButtonText"), "user_set_video_send_users"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("AutoSendTimeButtonText"), "user_set_auto_send_video_time"),
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
                InlineKeyboardButton.WithCallbackData(GetResourceString("AllContactsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_ALL_CONTACTS}"),
                InlineKeyboardButton.WithCallbackData(GetResourceString("DefaultGroupsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("SpecifiedContactsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS}"),
                InlineKeyboardButton.WithCallbackData(GetResourceString("SpecifiedGroupsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("OnlyMeButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_ONLY_TO_ME}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("DisableAutoSendButtonText"), $"user_set_video_send_users:{UsersAction.OFF}"),
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
                InlineKeyboardButton.WithCallbackData(GetResourceString("FiveSecondsButtonText"), "user_set_auto_send_video_time_to:5"),
                InlineKeyboardButton.WithCallbackData(GetResourceString("TenSecondsButtonText"), "user_set_auto_send_video_time_to:10"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("ThirtySecondsButtonText"), "user_set_auto_send_video_time_to:30"),
                InlineKeyboardButton.WithCallbackData(GetResourceString("OneMinuteButtonText"), "user_set_auto_send_video_time_to:60"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(GetResourceString("TenMinutesButtonText"), "user_set_auto_send_video_time_to:600"),
                InlineKeyboardButton.WithCallbackData(GetResourceString("SixtyMinutesButtonText"), "user_set_auto_send_video_time_to:3600"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("video_default_actions_menu")
            },
        });
        return inlineKeyboard;
    }
}
