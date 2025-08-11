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

    public VideoDefaultActionsMenuCommand(
        IUserGetter userGetter,
        IDefaultActionGetter defaultActionGetter,
        IGroupGetter groupGetter)
    {
        _userGetter = userGetter;
        _defaultActionGetter = defaultActionGetter;
        _groupGetter = groupGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        string da = _defaultActionGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        string action = string.Empty; string condition = string.Empty;
        if (!string.IsNullOrEmpty(da) && da != UsersAction.NO_VALUE && da.Contains(';'))
        {
            var parts = da.Split(';');
            action = parts.ElementAtOrDefault(0) ?? string.Empty;
            condition = parts.ElementAtOrDefault(1) ?? string.Empty;
        }
        int actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        var users = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
        var groupIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
        var groupNames = new List<string>();
        foreach (var gid in groupIds)
        {
            try { groupNames.Add($"{await _groupGetter.GetGroupNameById(gid)} (ID: {gid})"); } catch {}
        }
        string preface = $"<b>{Users.GetResourceString("Summary.Defaults.Header")}</b>\n" +
                         $"{Users.GetResourceString("Summary.Defaults.Action")}: <code>{action}</code>\n" +
                         $"{Users.GetResourceString("Summary.Defaults.Timeout")}: <code>{condition}</code>\n" +
                         $"{Users.GetResourceString("Summary.Defaults.Users")}: <code>{string.Join(", ", users)}</code>\n" +
                         (groupNames.Count > 0 ? $"{Users.GetResourceString("Summary.Defaults.Groups")}: {string.Join(", ", groupNames)}\n\n" : "\n");
        await Users.ViewVideoDefaultActionsMenu(botClient, update, preface);
    }
}

internal static class DefaultSummaries
{
    public static async Task<string> BuildDefaultsSummary(Update update)
    {
        try
        {
            long chatId = update.CallbackQuery!.Message!.Chat.Id;
            using var scope = FluentDBMigrator.GetCurrentServiceProvider("sqlite", new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()).CreateScope();
            var userGetter = scope.ServiceProvider.GetRequiredService<IUserGetter>();
            var defaultGetter = scope.ServiceProvider.GetRequiredService<IDefaultActionGetter>();
            var groupGetter = scope.ServiceProvider.GetRequiredService<IGroupGetter>();
            int userId = userGetter.GetUserIDbyTelegramID(chatId);
            string da = defaultGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            string action = ""; string condition = "";
            if (!string.IsNullOrEmpty(da) && da != UsersAction.NO_VALUE && da.Contains(';'))
            {
                var parts = da.Split(';');
                action = parts.ElementAtOrDefault(0) ?? "";
                condition = parts.ElementAtOrDefault(1) ?? "";
            }
            int actionId = defaultGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            var users = defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
            var groups = defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
            var groupNames = new List<string>();
            foreach (var gid in groups) { try { groupNames.Add($"{await groupGetter.GetGroupNameById(gid)} (ID: {gid})"); } catch {} }
            return $"<b>{Users.GetResourceString("Summary.Defaults.Header")}</b>\n" +
                   $"{Users.GetResourceString("Summary.Defaults.Action")}: <code>{action}</code>\n" +
                   $"{Users.GetResourceString("Summary.Defaults.Timeout")}: <code>{condition}</code>\n" +
                   $"{Users.GetResourceString("Summary.Defaults.Users")}: <code>{string.Join(", ", users)}</code>\n" +
                   (groupNames.Count > 0 ? $"{Users.GetResourceString("Summary.Defaults.Groups")}: {string.Join(", ", groupNames)}\n\n" : "\n");
        }
        catch { return string.Empty; }
    }

    public static async Task<string> BuildTargetsSummary(Update update)
    {
        try
        {
            long chatId = update.CallbackQuery!.Message!.Chat.Id;
            using var scope = FluentDBMigrator.GetCurrentServiceProvider("sqlite", new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()).CreateScope();
            var userGetter = scope.ServiceProvider.GetRequiredService<IUserGetter>();
            var defaultGetter = scope.ServiceProvider.GetRequiredService<IDefaultActionGetter>();
            var groupGetter = scope.ServiceProvider.GetRequiredService<IGroupGetter>();
            int userId = userGetter.GetUserIDbyTelegramID(chatId);
            int actionId = defaultGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            var users = defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
            var groups = defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
            var groupNames = new List<string>();
            foreach (var gid in groups) { try { groupNames.Add($"{await groupGetter.GetGroupNameById(gid)} (ID: {gid})"); } catch {} }
            return $"{Users.GetResourceString("Summary.Defaults.Users")}: <code>{string.Join(", ", users)}</code>\n" +
                   (groupNames.Count > 0 ? $"{Users.GetResourceString("Summary.Defaults.Groups")}: {string.Join(", ", groupNames)}\n\n" : "\n");
        }
        catch { return string.Empty; }
    }

    public static Task<string> BuildTimeoutSummary(Update update)
    {
        try
        {
            long chatId = update.CallbackQuery!.Message!.Chat.Id;
            using var scope = FluentDBMigrator.GetCurrentServiceProvider("sqlite", new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()).CreateScope();
            var userGetter = scope.ServiceProvider.GetRequiredService<IUserGetter>();
            var defaultGetter = scope.ServiceProvider.GetRequiredService<IDefaultActionGetter>();
            int userId = userGetter.GetUserIDbyTelegramID(chatId);
            string da = defaultGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            string condition = string.Empty;
            if (!string.IsNullOrEmpty(da) && da != UsersAction.NO_VALUE && da.Contains(';'))
            {
                var parts = da.Split(';');
                condition = parts.ElementAtOrDefault(1) ?? string.Empty;
            }
            return Task.FromResult($"{Users.GetResourceString("Summary.Defaults.Timeout")}: <code>{condition}</code>\n\n");
        }
        catch { return Task.FromResult(string.Empty); }
    }
}
public class UserSetAutoSendVideoTimeCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_auto_send_video_time";
    private readonly IUserGetter _userGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;

    public UserSetAutoSendVideoTimeCommand(
        IUserGetter userGetter,
        IDefaultActionGetter defaultActionGetter)
    {
        _userGetter = userGetter;
        _defaultActionGetter = defaultActionGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        string da = _defaultActionGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        string condition = string.Empty;
        if (!string.IsNullOrEmpty(da) && da != UsersAction.NO_VALUE && da.Contains(';'))
        {
            var parts = da.Split(';');
            condition = parts.ElementAtOrDefault(1) ?? string.Empty;
        }
        string preface = $"{Users.GetResourceString("Summary.Defaults.Timeout")}: <code>{condition}</code>\n\n";
        await Users.ViewAutoSendVideoTimeMenu(botClient, update, preface);
    }
}

public class UserSetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_video_send_users";
    private readonly IUserGetter _userGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly IGroupGetter _groupGetter;

    public UserSetVideoSendUsersCommand(
        IUserGetter userGetter,
        IDefaultActionGetter defaultActionGetter,
        IGroupGetter groupGetter)
    {
        _userGetter = userGetter;
        _defaultActionGetter = defaultActionGetter;
        _groupGetter = groupGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        int actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        var users = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
        var groupIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
        var groupNames = new List<string>();
        foreach (var gid in groupIds)
        {
            try { groupNames.Add($"{await _groupGetter.GetGroupNameById(gid)} (ID: {gid})"); } catch {}
        }
        string preface = $"{Users.GetResourceString("Summary.Defaults.Users")}: <code>{string.Join(", ", users)}</code>\n" +
                         (groupNames.Count > 0 ? $"{Users.GetResourceString("Summary.Defaults.Groups")}: {string.Join(", ", groupNames)}\n\n" : "\n");
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

    public PrivacySafetyMenuCommand(
        IUserGetter userGetter,
        IPrivacySettingsGetter privacyGetter,
        IPrivacySettingsTargetsGetter privacyTargetsGetter)
    {
        _userGetter = userGetter;
        _privacyGetter = privacyGetter;
        _privacyTargetsGetter = privacyTargetsGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        // Build summary for privacy
        var enabled = new List<string>();
        if (_privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SOCIAL_SITE_FILTER)) enabled.Add("Social");
        if (_privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.NSFW_SITE_FILTER)) enabled.Add("NSFW");
        if (_privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.UNIFIED_SITE_FILTER)) enabled.Add("Unified");
        bool domainsOn = _privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
        string domainInfo = domainsOn ? "Domains: ON" : "Domains: OFF";
        string preface = $"<b>Privacy:</b> {string.Join(", ", enabled)}\n{domainInfo}\n\n";
        await Users.ViewPrivacyMenu(botClient, update, preface);
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
        UsersDB.UpdateSelfLinkWithKeepSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository, _userRepository, _userGetter, new TelegramMediaRelayBot.Config.Services.ResourceService());
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
        UsersDB.UpdateSelfLinkWithDeleteSelectedContacts(update, _contactRemoverRepository, _contactGetterRepository, _userRepository, _userGetter, new TelegramMediaRelayBot.Config.Services.ResourceService());
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