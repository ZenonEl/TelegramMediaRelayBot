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

    public static InlineKeyboardMarkup GetSettingsKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("DefaultActionsButtonText"), "default_actions_menu"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("PrivacyButtonText"), "privacy_menu_and_safety"),
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
                InlineKeyboardButton.WithCallbackData(Localization.Get("SelfLinkRefreshButtonText"), "user_update_self_link"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("WhoCanFindMeByLink"), "user_update_who_can_find_me_by_link"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("SiteStopList"), "user_update_site_stop_list"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("AllowContentForwardingBtn"), "user_update_content_forwarding_rule"),
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
                InlineKeyboardButton.WithCallbackData(Localization.Get("SelfLinkRefreshButtonTextOption1"), "user_update_self_link_with_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("SelfLinkRefreshButtonTextOption2"), "user_update_self_link_with_new_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("SelfLinkRefreshButtonTextOption3"), "user_update_self_link_with_keep_selected_contacts"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("SelfLinkRefreshButtonTextOption4"), "user_update_self_link_with_delete_selected_contacts"),
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
                InlineKeyboardButton.WithCallbackData(Localization.Get("Enable"), "user_allow_content_forwarding"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("Disable"), "user_disallow_content_forwarding"),
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
                    Localization.Get("SocialFilterButton"), 
                    "user_set_site_stop_list:social"
                ), 
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    Localization.Get("NSFWFilterButton"),
                    "user_set_site_stop_list:nsfw"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    Localization.Get("UnifiedFilterButton"),
                    "user_set_site_stop_list:unified"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    Localization.Get("DomainFilterButton"),
                    "user_set_site_stop_list:domains"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    Localization.Get("ConfigureDomainsButton"),
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
                    Localization.Get("AddDomainButton"),
                    "user_set_site_stop_list:add_domains"
                ),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    Localization.Get("RemoveDomainButton"),
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
    public static InlineKeyboardMarkup GetDefaultActionsMenuKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("DefaultVideoActionsButtonText"), "video_default_actions_menu"),
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
                InlineKeyboardButton.WithCallbackData(Localization.Get("VideoRecipientsButtonText"), "user_set_video_send_users"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("AutoSendTimeButtonText"), "user_set_auto_send_video_time"),
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
                InlineKeyboardButton.WithCallbackData(Localization.Get("AllContactsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_ALL_CONTACTS}"),
                InlineKeyboardButton.WithCallbackData(Localization.Get("DefaultGroupsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("SpecifiedContactsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS}"),
                InlineKeyboardButton.WithCallbackData(Localization.Get("SpecifiedGroupsButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("OnlyMeButtonText"), $"user_set_video_send_users:{UsersAction.SEND_MEDIA_ONLY_TO_ME}"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("EnableAutoSendButtonText"), "user_set_video_send_users:enable"),
                InlineKeyboardButton.WithCallbackData(Localization.Get("DisableAutoSendButtonText"), $"user_set_video_send_users:{UsersAction.OFF}"),
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
                InlineKeyboardButton.WithCallbackData(Localization.Get("FiveSecondsButtonText"), "user_set_auto_send_video_time_to:5"),
                InlineKeyboardButton.WithCallbackData(Localization.Get("TenSecondsButtonText"), "user_set_auto_send_video_time_to:10"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("ThirtySecondsButtonText"), "user_set_auto_send_video_time_to:30"),
                InlineKeyboardButton.WithCallbackData(Localization.Get("OneMinuteButtonText"), "user_set_auto_send_video_time_to:60"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Localization.Get("TenMinutesButtonText"), "user_set_auto_send_video_time_to:600"),
                InlineKeyboardButton.WithCallbackData(Localization.Get("SixtyMinutesButtonText"), "user_set_auto_send_video_time_to:3600"),
            },
            new[]
            {
                KeyboardUtils.GetReturnButton("video_default_actions_menu")
            },
        });
        return inlineKeyboard;
    }
}
