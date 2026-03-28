// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

/// <summary>
/// Contains constant names for all callback query handlers.
/// Use these instead of magic strings when referencing callback handler names.
/// </summary>
public static class CallbackNames
{
    // General
    public const string MainMenu = "main_menu";
    public const string GetSelfLink = "get_self_link";
    public const string WhosTheGenius = "whos_the_genius";

    // Contacts
    public const string AddContact = "add_contact";
    public const string ViewInboundInviteLinks = "view_inbound_invite_links";
    public const string ViewOutboundInviteLinks = "view_outbound_invite_links";
    public const string ShowGroups = "show_groups";
    public const string EditContactGroup = "edit_contact_group";
    public const string ViewContacts = "view_contacts";
    public const string MuteContact = "mute_contact";
    public const string UnmuteContact = "unmute_contact";
    public const string RenameContact = "edit_contact_name";
    public const string DeleteContact = "delete_contact";

    // Settings
    public const string ShowSettings = "show_settings";
    public const string DefaultActionsMenu = "default_actions_menu";
    public const string VideoDefaultActionsMenu = "video_default_actions_menu";
    public const string UserSetAutoSendVideoTime = "user_set_auto_send_video_time";
    public const string UserSetVideoSendUsers = "user_set_video_send_users";

    // Privacy & Safety
    public const string PrivacyMenuAndSafety = "privacy_menu_and_safety";
    public const string UserUpdateSelfLink = "user_update_self_link";
    public const string UserUpdateSelfLinkWithContacts = "user_update_self_link_with_contacts";
    public const string UserUpdateSelfLinkWithNewContacts = "user_update_self_link_with_new_contacts";
    public const string UserUpdateSelfLinkWithKeepSelectedContacts = "user_update_self_link_with_keep_selected_contacts";
    public const string UserUpdateSelfLinkWithDeleteSelectedContacts = "user_update_self_link_with_delete_selected_contacts";
    public const string ProcessUserUpdateSelfLinkWithContacts = "process_user_update_self_link_with_contacts";
    public const string UserUpdateWhoCanFindMeByLink = "user_update_who_can_find_me_by_link";
    public const string UserSetNobodyWhoCanFindMeByLink = "user_set_nobody_who_can_find_me_by_link";
    public const string UserSetGeneralWhoCanFindMeByLink = "user_set_general_who_can_find_me_by_link";
    public const string UserSetAllWhoCanFindMeByLink = "user_set_all_who_can_find_me_by_link";
    public const string ProcessUserUpdateSelfLinkWithNewContacts = "process_user_update_self_link_with_new_contacts";
    public const string UserUpdateContentForwardingRule = "user_update_content_forwarding_rule";
    public const string UserDisallowContentForwarding = "user_disallow_content_forwarding";
    public const string UserAllowContentForwarding = "user_allow_content_forwarding";
    public const string UserUpdateSiteStopList = "user_update_site_stop_list";
    public const string UserSetSiteStopListSettings = "user_set_site_stop_list_settings";

    // Parameterized (include trailing colon for prefix matching)
    public const string UserShowOutboundInvite = "user_show_outbound_invite:";
    public const string UserSetAutoSendVideoTimeTo = "user_set_auto_send_video_time_to:";
    public const string UserSetVideoSendUsersParameterized = "user_set_video_send_users:";
    public const string UserSetSiteStopList = "user_set_site_stop_list:";
}
