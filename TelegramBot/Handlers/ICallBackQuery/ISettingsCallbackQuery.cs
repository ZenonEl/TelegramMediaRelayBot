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
    public string Name => "user_update_self_link_with_new_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
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

    public UserUpdateSelfLinkWithKeepSelectedContactsCommand(
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository)
    {
        _contactRemoverRepository = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
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
        UsersDB.UpdateSelfLinkWithKeepSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository);
    }
}

public class UserUpdateSelfLinkWithDeleteSelectedContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRemoverRepository;
    private readonly IContactGetter _contactGetterRepository;    

    public UserUpdateSelfLinkWithDeleteSelectedContactsCommand(
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository)
    {
        _contactRemoverRepository = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
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
        UsersDB.UpdateSelfLinkWithDeleteSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository);
    }
}

public class ProcessUserUpdateSelfLinkWithContactsCommand : IBotCallbackQueryHandlers
{
    public string Name => "process_user_update_self_link_with_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        UsersDB.UpdateSelfLinkWithContacts(update);
        await Users.ViewLinkPrivacyMenu(botClient, update);
    }
}

public class ProcessUserUpdateSelfLinkWithNewContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRepository;

    public ProcessUserUpdateSelfLinkWithNewContactsCommand(IContactRemover contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public string Name => "process_user_update_self_link_with_new_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        UsersDB.UpdateSelfLinkWithNewContacts(update, _contactRepository);
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
    public string Name => "user_disallow_content_forwarding";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id);
        bool actionStatus = PrivacySettingsSetter.SetPrivacyRuleToDisabled(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING);
        string statusMessage = actionStatus
            ? Config.GetResourceString("SuccessActionResult")
            : Config.GetResourceString("ErrorActionResult");
        await Users.ViewPrivacyMenu(botClient, update, statusMessage);
    }
}

public class UserEnablePermanentContentSpoilerCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_allow_content_forwarding";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id);
        bool actionStatus = PrivacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING, "disallow_content_forwarding", true, "always");
        string statusMessage = actionStatus
            ? Config.GetResourceString("SuccessActionResult")
            : Config.GetResourceString("ErrorActionResult");
        await Users.ViewPrivacyMenu(botClient, update, statusMessage);
    }
}