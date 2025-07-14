// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

namespace TelegramMediaRelayBot.Database;

public class ButtonData
{
    public required string ButtonText { get; set; }
    public required string CallbackData { get; set; }
}

public class PrivacyRuleResult
{
    public required string Type { get; set; }
    public required string Action { get; set; }
    public required string TargetValue { get; set; }
}

public class ContactsStatus
{
    public const string WAITING_FOR_ACCEPT = "waiting_for_accept";
    public const string ACCEPTED = "accepted";
    public const string DECLINED = "declined";
}

public class UsersActionTypes
{
    public const string DEFAULT_MEDIA_DISTRIBUTION = "default_media_distribution";
}

public class PrivacyRuleType
{
    public const string WHO_CAN_FIND_ME_BY_LINK = "who_can_find_me_by_link";
    public const string ALLOW_CONTENT_FORWARDING = "allow_forwarding";
    public const string SOCIAL_SITE_FILTER = "social_sites_filter";
    public const string NSFW_SITE_FILTER = "nsfw_sites_filter";
    public const string UNIFIED_SITE_FILTER = "unified_sites_filter";
    public const string SITES_BY_DOMAIN_FILTER = "sites_by_domain_filter";
}

public class PrivacyRuleAction
{
    public const string ALL_CAN_FIND_ME_BY_LINK = "all_can_find_me_by_link";
    public const string GENERAL_CAN_FIND_ME_BY_LINK = "general_can_find_me_by_link";
    public const string NOBODY_CAN_FIND_ME_BY_LINK = "nobody_can_find_me_by_link";
    public const string SOCIAL_FILTER = "block_social_sites";
    public const string NSFW_FILTER = "block_nsfw_sites";
    public const string UNIFIED_FILTER = "block_unified_sites";
    public const string DOMAIN_FILTER = "block_sites_by_domain";
}

public class UsersAction
{
    public const string NO_VALUE = "";
    public const string OFF = "off";
    public const string SEND_MEDIA_TO_ALL_CONTACTS = "send_to_all_contacts";
    public const string SEND_MEDIA_TO_DEFAULT_GROUPS = "send_to_default_groups";
    public const string SEND_MEDIA_TO_SPECIFIED_GROUPS = "send_to_specified_groups";
    public const string SEND_MEDIA_TO_SPECIFIED_USERS = "send_to_specified_users";
    public const string SEND_MEDIA_ONLY_TO_ME = "send_only_to_me";
}

public class TargetTypes
{
    public const string GROUP = "group";
    public const string USER = "user";
}
