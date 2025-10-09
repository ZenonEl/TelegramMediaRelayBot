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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Menu;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;


namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class ShowSettingsCommand : IBotCallbackQueryHandlers
{
    public string Name => "show_settings";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewSettings(botClient, update);
    }
}

//------------------------------------------------------------------------------------------------
//DEFAULT ACTIONS
//------------------------------------------------------------------------------------------------


public class DefaultActionsMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "default_actions_menu";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewDefaultActionsMenu(botClient, update);
    }
}

public class VideoDefaultActionsMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "video_default_actions_menu";
    private readonly IUserGetter _userGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService _summary;

    public VideoDefaultActionsMenuCommand(
        IUserGetter userGetter,
        IDefaultActionGetter defaultActionGetter,
        IGroupGetter groupGetter,
        TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService summary)
    {
        _userGetter = userGetter;
        _defaultActionGetter = defaultActionGetter;
        _groupGetter = groupGetter;
        _summary = summary;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildDefaultsSummary(update);
        await Users.ViewVideoDefaultActionsMenu(botClient, update, preface);
    }
}
public class UserSetAutoSendVideoTimeCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_auto_send_video_time";
    private readonly IUserGetter _userGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService _summary;

    public UserSetAutoSendVideoTimeCommand(
        IUserGetter userGetter,
        IDefaultActionGetter defaultActionGetter,
        TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService summary)
    {
        _userGetter = userGetter;
        _defaultActionGetter = defaultActionGetter;
        _summary = summary;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildTimeoutSummary(update);
        await Users.ViewAutoSendVideoTimeMenu(botClient, update, preface);
    }
}

public class UserSetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_video_send_users";
    private readonly IUserGetter _userGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService _summary;

    public UserSetVideoSendUsersCommand(
        IUserGetter userGetter,
        IDefaultActionGetter defaultActionGetter,
        IGroupGetter groupGetter,
        TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService summary)
    {
        _userGetter = userGetter;
        _defaultActionGetter = defaultActionGetter;
        _groupGetter = groupGetter;
        _summary = summary;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildTargetsSummary(update);
        await Users.ViewUsersVideoSentUsersActionsMenu(botClient, update, preface);
    }
}

//------------------------------------------------------------------------------------------------
//PRIVACY & SAFETY
//------------------------------------------------------------------------------------------------


public class PrivacySafetyMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "privacy_menu_and_safety";
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsGetter _privacyGetter;
    private readonly IPrivacySettingsTargetsGetter _privacyTargetsGetter;
    private readonly TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService _summary;

    public PrivacySafetyMenuCommand(
        IUserGetter userGetter,
        IPrivacySettingsGetter privacyGetter,
        IPrivacySettingsTargetsGetter privacyTargetsGetter,
        TelegramMediaRelayBot.TelegramBot.Services.IDefaultSummaryService summary)
    {
        _userGetter = userGetter;
        _privacyGetter = privacyGetter;
        _privacyTargetsGetter = privacyTargetsGetter;
        _summary = summary;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildPrivacySummary(update);
        await Users.ViewPrivacyMenu(botClient, update, preface);
    }
}

public class UserInboxMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_inbox_menu";
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsGetter _privacyGetter;
    public UserInboxMenuCommand(IUserGetter userGetter, IPrivacySettingsGetter privacyGetter)
    {
        _userGetter = userGetter;
        _privacyGetter = privacyGetter;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        bool inboxOn = _privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.INBOX_DELIVERY);
        await CommonUtilities.SendMessage(botClient, update, UsersPrivacyMenuKB.GetInboxKeyboardMarkup(inboxOn), ct, Users.GetResourceString("InboxSettingsTitle"));
    }
}

public class UserInboxEnableCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_inbox_enable";
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySetter;
    public UserInboxEnableCommand(IUserGetter userGetter, IPrivacySettingsSetter privacySetter)
    {
        _userGetter = userGetter;
        _privacySetter = privacySetter;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        await _privacySetter.SetPrivacyRule(userId, PrivacyRuleType.INBOX_DELIVERY, PrivacyRuleAction.USE_INBOX, true, "always");
        await Users.ViewPrivacyMenu(botClient, update, Users.GetResourceString("InboxOn"));
    }
}

public class UserInboxDisableCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_inbox_disable";
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySetter;
    public UserInboxDisableCommand(IUserGetter userGetter, IPrivacySettingsSetter privacySetter)
    {
        _userGetter = userGetter;
        _privacySetter = privacySetter;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        await _privacySetter.SetPrivacyRuleToDisabled(userId, PrivacyRuleType.INBOX_DELIVERY);
        await Users.ViewPrivacyMenu(botClient, update, Users.GetResourceString("InboxOff"));
    }
}

public class UserUpdateSelfLinkCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_update_self_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewLinkPrivacyMenu(botClient, update);
    }
}

public class UserUpdateSelfLinkWithContactsCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_update_self_link_with_contacts";


    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CommonUtilities.SendMessage(botClient, update,
            KeyboardUtils.GetConfirmForActionKeyboardMarkup(
                $"process_user_update_self_link_with_contacts",
                $"user_update_self_link"),
            ct,
            Users.GetResourceString("UpdateLinkKeepContactsConfirmation"));
    }
}

public class UserUpdateSelfLinkWithNewContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    public string Name => "user_update_self_link_with_new_contacts";

    public UserUpdateSelfLinkWithNewContactsCommand(
        IUserGetter userGetter)
    {
        _userGetter = userGetter;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        await CommonUtilities.SendMessage(botClient, update,
            KeyboardUtils.GetConfirmForActionKeyboardMarkup(
                $"process_user_update_self_link_with_new_contacts",
                $"user_update_self_link"),
            ct,
            Users.GetResourceString("UpdateLinkNewContactsWarning"));
    }
}

public class UserUpdateSelfLinkWithKeepSelectedContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRemoverRepository;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public UserUpdateSelfLinkWithKeepSelectedContactsCommand(
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        _contactRemoverRepository = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
        _userRepository = userRepository;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public string Name => "user_update_self_link_with_keep_selected_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int ownerUserId = _userGetter.GetUserIDbyTelegramID(chatId);
        var tgIds = await _contactGetterRepository.GetAllContactUserTGIds(ownerUserId);
        var lines = new List<string>();
        foreach (var tg in tgIds)
        {
            int cid = _userGetter.GetUserIDbyTelegramID(tg);
            string uname = _userGetter.GetUserNameByTelegramID(tg);
            string link = _userGetter.GetUserSelfLink(tg);
            lines.Add(string.Format(Users.GetResourceString("ContactInfo"), cid, uname, link));
        }
        string header = Users.GetResourceString("YourContacts");
        string body = lines.Count > 0 ? string.Join("\n", lines) : Users.GetResourceString("NoUsersFound");
        string prompt = Users.GetResourceString("EnterContactIdsPrompt");
        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
            ct,
            $"{header}\n{body}\n\n{prompt}");
        UsersDB.UpdateSelfLinkWithKeepSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository, _userRepository, _userGetter, _resourceService);
    }
}

public class UserUpdateSelfLinkWithDeleteSelectedContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRemoverRepository;
    private readonly IContactGetter _contactGetterRepository;    
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public UserUpdateSelfLinkWithDeleteSelectedContactsCommand(
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        _contactRemoverRepository = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
        _userRepository = userRepository;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public string Name => "user_update_self_link_with_delete_selected_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int ownerUserId = _userGetter.GetUserIDbyTelegramID(chatId);
        var tgIds = await _contactGetterRepository.GetAllContactUserTGIds(ownerUserId);
        var lines = new List<string>();
        foreach (var tg in tgIds)
        {
            int cid = _userGetter.GetUserIDbyTelegramID(tg);
            string uname = _userGetter.GetUserNameByTelegramID(tg);
            string link = _userGetter.GetUserSelfLink(tg);
            lines.Add(string.Format(Users.GetResourceString("ContactInfo"), cid, uname, link));
        }
        string header = Users.GetResourceString("YourContacts");
        string body = lines.Count > 0 ? string.Join("\n", lines) : Users.GetResourceString("NoUsersFound");
        string prompt = Users.GetResourceString("EnterContactIdsPrompt");
        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
            ct,
            $"{header}\n{body}\n\n{prompt}");
        UsersDB.UpdateSelfLinkWithDeleteSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository, _userRepository, _userGetter, _resourceService);
    }
}

public class ProcessUserUpdateSelfLinkWithContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;

    public ProcessUserUpdateSelfLinkWithContactsCommand(
        IUserRepository userRepository,
        IUserGetter userGetter)
    {
        _userRepository = userRepository;
        _userGetter = userGetter;
    }

    public string Name => "process_user_update_self_link_with_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        UsersDB.UpdateSelfLinkWithContacts(update, _userRepository, _userGetter);
        await Users.ViewLinkPrivacyMenu(botClient, update);
    }
}

public class ProcessUserUpdateWhoCanFindMeByLinkCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_update_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewWhoCanFindMeByLinkMenu(botClient, update);
    }
}

public class ProcessUserSetNobodyWhoCanFindMeByLinkCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySettingsSetter;

    public ProcessUserSetNobodyWhoCanFindMeByLinkCommand(
        IUserGetter userGetter,
        IPrivacySettingsSetter privacySettingsSetter
    )
    {
        _userGetter = userGetter;
        _privacySettingsSetter = privacySettingsSetter;
    }

    public string Name => "user_set_nobody_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long telegramId = CommonUtilities.GetIDfromUpdate(update);
        int userId = _userGetter.GetUserIDbyTelegramID(telegramId);
        await _privacySettingsSetter.SetPrivacyRule(
            userId,
            PrivacyRuleType.WHO_CAN_FIND_ME_BY_LINK,
            PrivacyRuleAction.NOBODY_CAN_FIND_ME_BY_LINK,
            true,
            "always");
    }
}

public class ProcessUserSetGeneralWhoCanFindMeByLinkCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySettingsSetter;

    public ProcessUserSetGeneralWhoCanFindMeByLinkCommand(
        IUserGetter userGetter,
        IPrivacySettingsSetter privacySettingsSetter
    )
    {
        _userGetter = userGetter;
        _privacySettingsSetter = privacySettingsSetter;
    }

    public string Name => "user_set_general_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long telegramId = CommonUtilities.GetIDfromUpdate(update);
        int userId = _userGetter.GetUserIDbyTelegramID(telegramId);
        await _privacySettingsSetter.SetPrivacyRule(
            userId,
            PrivacyRuleType.WHO_CAN_FIND_ME_BY_LINK,
            PrivacyRuleAction.GENERAL_CAN_FIND_ME_BY_LINK,
            true,
            "always");
    }
}

public class ProcessUserSetAllWhoCanFindMeByLinkCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySettingsSetter;

    public ProcessUserSetAllWhoCanFindMeByLinkCommand(
        IUserGetter userGetter,
        IPrivacySettingsSetter privacySettingsSetter
    )
    {
        _userGetter = userGetter;
        _privacySettingsSetter = privacySettingsSetter;
    }

    public string Name => "user_set_all_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long telegramId = CommonUtilities.GetIDfromUpdate(update);
        int userId = _userGetter.GetUserIDbyTelegramID(telegramId);
        await _privacySettingsSetter.SetPrivacyRule(
            userId,
            PrivacyRuleType.WHO_CAN_FIND_ME_BY_LINK,
            PrivacyRuleAction.ALL_CAN_FIND_ME_BY_LINK,
            true,
            "always");
    }
}

public class ProcessUserUpdateSelfLinkWithNewContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;

    public ProcessUserUpdateSelfLinkWithNewContactsCommand(
        IContactRemover contactRepository,
        IUserRepository userRepository,
        IUserGetter userGetter)
    {
        _contactRepository = contactRepository;
        _userRepository = userRepository;
        _userGetter = userGetter;
    }

    public string Name => "process_user_update_self_link_with_new_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        UsersDB.UpdateSelfLinkWithNewContacts(update, _contactRepository, _userRepository, _userGetter);
        await Users.ViewLinkPrivacyMenu(botClient, update);
    }
}

public class UserUpdatePermanentContentSpoilerCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_update_content_forwarding_rule";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewPermanentContentSpoilerMenu(botClient, update);
    }
}

public class UserDisablePermanentContentSpoilerCommand : IBotCallbackQueryHandlers
{
    private readonly IPrivacySettingsSetter _privacySettingsSetter;
    private readonly IUserGetter _userGetter;

    public UserDisablePermanentContentSpoilerCommand(
        IPrivacySettingsSetter privacySettingsSetter,
        IUserGetter userGetter)
    {
        _privacySettingsSetter = privacySettingsSetter;
        _userGetter = userGetter;
    }

    public string Name => "user_disallow_content_forwarding";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id);
        bool actionStatus = await _privacySettingsSetter.SetPrivacyRuleToDisabled(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING);
        string statusMessage = actionStatus
            ? Users.GetResourceString("SuccessActionResult")
            : Users.GetResourceString("ErrorActionResult");
        await Users.ViewPrivacyMenu(botClient, update, statusMessage);
    }
}

public class UserEnablePermanentContentSpoilerCommand : IBotCallbackQueryHandlers
{
    private readonly IPrivacySettingsSetter _privacySettingsSetter;
    private readonly IUserGetter _userGetter;

    public UserEnablePermanentContentSpoilerCommand(
        IPrivacySettingsSetter privacySettingsSetter,
        IUserGetter userGetter)
    {
        _privacySettingsSetter = privacySettingsSetter;
        _userGetter = userGetter;
    }

    public string Name => "user_allow_content_forwarding";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id);
        bool actionStatus = await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING, "disallow_content_forwarding", true, "always");
        string statusMessage = actionStatus
            ? Users.GetResourceString("SuccessActionResult")
            : Users.GetResourceString("ErrorActionResult");
        await Users.ViewPrivacyMenu(botClient, update, statusMessage);
    }
}

public class UserUpdateSiteStopListCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_update_site_stop_list";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewSiteFilterMenu(botClient, update);
    }
}

public class UserSetSiteStopListSettingsCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_site_stop_list_settings";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewSiteFilterSettingsMenu(botClient, update);
    }
}