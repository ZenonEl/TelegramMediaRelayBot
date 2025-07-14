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
using TelegramMediaRelayBot.TelegramBot.Menu;
using TelegramMediaRelayBot.TelegramBot.Utils;


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

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewVideoDefaultActionsMenu(botClient, update);
    }
}

public class UserSetAutoSendVideoTimeCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_auto_send_video_time";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewAutoSendVideoTimeMenu(botClient, update);
    }
}

public class UserSetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_video_send_users";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewUsersVideoSentUsersActionsMenu(botClient, update);
    }
}

//------------------------------------------------------------------------------------------------
//PRIVACY & SAFETY
//------------------------------------------------------------------------------------------------


public class PrivacySafetyMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "privacy_menu_and_safety";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Users.ViewPrivacyMenu(botClient, update);
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
            Config.GetResourceString("UpdateLinkKeepContactsConfirmation"));
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
            Config.GetResourceString("UpdateLinkNewContactsWarning"));
    }
}

public class UserUpdateSelfLinkWithKeepSelectedContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRemoverRepository;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;

    public UserUpdateSelfLinkWithKeepSelectedContactsCommand(
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter)
    {
        _contactRemoverRepository = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
        _userRepository = userRepository;
        _userGetter = userGetter;
    }

    public string Name => "user_update_self_link_with_keep_selected_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
            ct,
            Config.GetResourceString("EnterContactIdsPrompt"));
        UsersDB.UpdateSelfLinkWithKeepSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository, _userRepository, _userGetter);
    }
}

public class UserUpdateSelfLinkWithDeleteSelectedContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRemoverRepository;
    private readonly IContactGetter _contactGetterRepository;    
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;

    public UserUpdateSelfLinkWithDeleteSelectedContactsCommand(
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter)
    {
        _contactRemoverRepository = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
        _userRepository = userRepository;
        _userGetter = userGetter;
    }

    public string Name => "user_update_self_link_with_delete_selected_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
            ct,
            Config.GetResourceString("EnterContactIdsPrompt"));
        UsersDB.UpdateSelfLinkWithDeleteSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository, _userRepository, _userGetter);
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
        bool actionStatus = _privacySettingsSetter.SetPrivacyRuleToDisabled(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING);
        string statusMessage = actionStatus
            ? Config.GetResourceString("SuccessActionResult")
            : Config.GetResourceString("ErrorActionResult");
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
            ? Config.GetResourceString("SuccessActionResult")
            : Config.GetResourceString("ErrorActionResult");
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