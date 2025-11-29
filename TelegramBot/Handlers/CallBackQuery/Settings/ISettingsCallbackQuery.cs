// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.


using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;


namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class ShowSettingsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    public string Name => "show_settings";

    public ShowSettingsCommand(IUserMenuService menuService)
    {
        _menuService = menuService;
    }

    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _menuService.ViewSettings(botClient, update);
    }
}

//------------------------------------------------------------------------------------------------
//DEFAULT ACTIONS
//------------------------------------------------------------------------------------------------


public class DefaultActionsMenuCommand(IUserMenuService menuService) : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService = menuService;
    public string Name => "default_actions_menu";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _menuService.ViewDefaultActionsMenu(botClient, update);
    }
}

public class VideoDefaultActionsMenuCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    public string Name => "video_default_actions_menu";
    private readonly IDefaultSummaryService _summary;

    public VideoDefaultActionsMenuCommand(
        IDefaultSummaryService summary,
        IUserMenuService menuService)
    {
        _summary = summary;
        _menuService = menuService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildDefaultsSummary(update);
        await _menuService.ViewVideoDefaultActionsMenu(botClient, update, preface);
    }
}
public class UserSetAutoSendVideoTimeCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    public string Name => "user_set_auto_send_video_time";
    private readonly IDefaultSummaryService _summary;


    public UserSetAutoSendVideoTimeCommand(
        IDefaultSummaryService summary,
        IUserMenuService menuService)
    {
        _summary = summary;
        _menuService = menuService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildTimeoutSummary(update);
        await _menuService.ViewAutoSendVideoTimeMenu(botClient, update, preface);
    }
}

public class UserSetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    public string Name => "user_set_video_send__menuService";
    private readonly IDefaultSummaryService _summary;

    public UserSetVideoSendUsersCommand(
        IDefaultSummaryService summary,
        IUserMenuService menuService)
    {
        _summary = summary;
        _menuService = menuService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildTargetsSummary(update);
        await _menuService.ViewUsersVideoSentUsersActionsMenu(botClient, update, preface);
    }
}

//------------------------------------------------------------------------------------------------
//PRIVACY & SAFETY
//------------------------------------------------------------------------------------------------


public class PrivacySafetyMenuCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    private readonly IDefaultSummaryService _summary;
    public string Name => "privacy_menu_and_safety";

    public PrivacySafetyMenuCommand(
        IDefaultSummaryService summary,
        IUserMenuService menuService)
    {
        _summary = summary;
        _menuService = menuService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string preface = await _summary.BuildPrivacySummary(update);
        await _menuService.ViewPrivacyMenu(botClient, update, preface);
    }
}

public class UserInboxMenuCommand : IBotCallbackQueryHandlers
{
    private readonly IResourceService _resourceService;
    public string Name => "user_inbox_menu";
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsGetter _privacyGetter;
    private readonly ITelegramInteractionService _interactionService;

    public UserInboxMenuCommand(IUserGetter userGetter, IPrivacySettingsGetter privacyGetter, IResourceService resourceService, ITelegramInteractionService interactionService)
    {
        _userGetter = userGetter;
        _privacyGetter = privacyGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        bool inboxOn = _privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.INBOX_DELIVERY);
        await _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetInboxKeyboardMarkup(inboxOn), ct, _resourceService.GetResourceString("InboxSettingsTitle"));
    }
}

public class UserInboxEnableCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    private readonly IResourceService _resourceService;
    public string Name => "user_inbox_enable";
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySetter;
    public UserInboxEnableCommand(IUserGetter userGetter, IPrivacySettingsSetter privacySetter, IUserMenuService menuService, IResourceService resourceService)
    {
        _userGetter = userGetter;
        _privacySetter = privacySetter;
        _menuService = menuService;
        _resourceService = resourceService;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        await _privacySetter.SetPrivacyRule(userId, PrivacyRuleType.INBOX_DELIVERY, PrivacyRuleAction.USE_INBOX, true, "always");
        await _menuService.ViewPrivacyMenu(botClient, update, _resourceService.GetResourceString("InboxOn"));
    }
}

public class UserInboxDisableCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    private readonly IResourceService _resourceService;
    public string Name => "user_inbox_disable";
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySetter;
    public UserInboxDisableCommand(IUserGetter userGetter, IPrivacySettingsSetter privacySetter, IUserMenuService menuService, IResourceService resourceService)
    {
        _userGetter = userGetter;
        _privacySetter = privacySetter;
        _menuService = menuService;
        _resourceService = resourceService;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        await _privacySetter.SetPrivacyRuleToDisabled(userId, PrivacyRuleType.INBOX_DELIVERY);
        await _menuService.ViewPrivacyMenu(botClient, update, _resourceService.GetResourceString("InboxOff"));
    }
}

public class UserUpdateSelfLinkCommand(IUserMenuService menuService) : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService = menuService;
    public string Name => "user_update_self_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _menuService.ViewLinkPrivacyMenu(botClient, update);
    }
}

public class UserUpdateSelfLinkWithContactsCommand(IResourceService resourceService, ITelegramInteractionService interactionService) : IBotCallbackQueryHandlers
{
    private readonly IResourceService _resourceService = resourceService;
    private readonly ITelegramInteractionService _interactionService = interactionService;
    public string Name => "user_update_self_link_with_contacts";


    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _interactionService.ReplyToUpdate(botClient, update,
            KeyboardUtils.GetConfirmForActionKeyboardMarkup(
                $"process_user_update_self_link_with_contacts",
                $"user_update_self_link"),
            ct,
            _resourceService.GetResourceString("UpdateLinkKeepContactsConfirmation"));
    }
}

public class UserUpdateSelfLinkWithNewContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    public string Name => "user_update_self_link_with_new_contacts";

    public UserUpdateSelfLinkWithNewContactsCommand(
        IUserGetter userGetter,
        IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _userGetter = userGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        await _interactionService.ReplyToUpdate(botClient, update,
            KeyboardUtils.GetConfirmForActionKeyboardMarkup(
                $"process_user_update_self_link_with_new_contacts",
                $"user_update_self_link"),
            ct,
            _resourceService.GetResourceString("UpdateLinkNewContactsWarning"));
    }
}

public class UserUpdateSelfLinkWithKeepSelectedContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public UserUpdateSelfLinkWithKeepSelectedContactsCommand(
        IContactGetter contactGetterRepository,
        IUserGetter userGetter,
        IResourceService resourceService,
        IUserMenuService menuService,
        ITelegramInteractionService interactionService)
    {
        _menuService = menuService;
        _contactGetterRepository = contactGetterRepository;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
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
            lines.Add(string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, link));
        }
        string header = _resourceService.GetResourceString("YourContacts");
        string body = lines.Count > 0 ? string.Join("\n", lines) : _resourceService.GetResourceString("No_menuServiceFound");
        string prompt = _resourceService.GetResourceString("EnterContactIdsPrompt");
        await _interactionService.ReplyToUpdate(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
            ct,
            $"{header}\n{body}\n\n{prompt}");
        _menuService.UpdateSelfLinkWithKeepSelectedContacts(update);
    }
}

public class UserUpdateSelfLinkWithDeleteSelectedContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    private readonly IContactGetter _contactGetterRepository;    
    private readonly IResourceService _resourceService;
    private readonly IUserGetter _userGetter;
    private readonly ITelegramInteractionService _interactionService;

    public UserUpdateSelfLinkWithDeleteSelectedContactsCommand(
        IContactGetter contactGetterRepository,
        IUserGetter userGetter,
        IResourceService resourceService,
        IUserMenuService menuService,
        ITelegramInteractionService interactionService)
    {
        _contactGetterRepository = contactGetterRepository;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _menuService = menuService;
        _interactionService = interactionService;
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
            lines.Add(string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, link));
        }
        string header = _resourceService.GetResourceString("YourContacts");
        string body = lines.Count > 0 ? string.Join("\n", lines) : _resourceService.GetResourceString("No_menuServiceFound");
        string prompt = _resourceService.GetResourceString("EnterContactIdsPrompt");
        await _interactionService.ReplyToUpdate(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
            ct,
            $"{header}\n{body}\n\n{prompt}");
        _menuService.UpdateSelfLinkWithDeleteSelectedContacts(update);
    }
}

public class ProcessUserUpdateSelfLinkWithContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;

    public ProcessUserUpdateSelfLinkWithContactsCommand(
        IUserMenuService menuService)
    {
        _menuService = menuService;
    }

    public string Name => "process_user_update_self_link_with_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        _menuService.UpdateSelfLinkWithContacts(update);
        await _menuService.ViewLinkPrivacyMenu(botClient, update);
    }
}

public class ProcessUserUpdateWhoCanFindMeByLinkCommand(IUserMenuService menuService) : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService = menuService;
    public string Name => "user_update_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _menuService.ViewWhoCanFindMeByLinkMenu(botClient, update);
    }
}

public class ProcessUserSetNobodyWhoCanFindMeByLinkCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsSetter _privacySettingsSetter;
    private readonly ITelegramInteractionService _interactionService;

    public ProcessUserSetNobodyWhoCanFindMeByLinkCommand(
        IUserGetter userGetter,
        IPrivacySettingsSetter privacySettingsSetter,
        ITelegramInteractionService interactionService
    )
    {
        _userGetter = userGetter;
        _privacySettingsSetter = privacySettingsSetter;
        _interactionService = interactionService;
    }

    public string Name => "user_set_nobody_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long telegramId = _interactionService.GetChatId(update);
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
    private readonly ITelegramInteractionService _interactionService;

    public ProcessUserSetGeneralWhoCanFindMeByLinkCommand(
        IUserGetter userGetter,
        IPrivacySettingsSetter privacySettingsSetter,
        ITelegramInteractionService interactionService
    )
    {
        _userGetter = userGetter;
        _privacySettingsSetter = privacySettingsSetter;
        _interactionService = interactionService;
    }

    public string Name => "user_set_general_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long telegramId = _interactionService.GetChatId(update);
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
    private readonly ITelegramInteractionService _interactionService;

    public ProcessUserSetAllWhoCanFindMeByLinkCommand(
        IUserGetter userGetter,
        IPrivacySettingsSetter privacySettingsSetter,
        ITelegramInteractionService interactionService
    )
    {
        _userGetter = userGetter;
        _privacySettingsSetter = privacySettingsSetter;
        _interactionService = interactionService;
    }

    public string Name => "user_set_all_who_can_find_me_by_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long telegramId = _interactionService.GetChatId(update);
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
    private readonly IUserMenuService _menuService;

    public ProcessUserUpdateSelfLinkWithNewContactsCommand(
        IUserMenuService menuService)
    {
        _menuService = menuService;
    }

    public string Name => "process_user_update_self_link_with_new_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        _menuService.UpdateSelfLinkWithNewContacts(update);
        await _menuService.ViewLinkPrivacyMenu(botClient, update);
    }
}

public class UserUpdatePermanentContentSpoilerCommand(IUserMenuService menuService) : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService = menuService;
    public string Name => "user_update_content_forwarding_rule";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _menuService.ViewPermanentContentSpoilerMenu(botClient, update);
    }
}

public class UserDisablePermanentContentSpoilerCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    private readonly IPrivacySettingsSetter _privacySettingsSetter;
    private readonly IResourceService _resourceService;
    private readonly IUserGetter _userGetter;

    public UserDisablePermanentContentSpoilerCommand(
        IPrivacySettingsSetter privacySettingsSetter,
        IUserGetter userGetter,
        IResourceService resourceService,
        IUserMenuService menuService
    )
    {
        _privacySettingsSetter = privacySettingsSetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _menuService = menuService;
    }

    public string Name => "user_disallow_content_forwarding";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id);
        bool actionStatus = await _privacySettingsSetter.SetPrivacyRuleToDisabled(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING);
        string statusMessage = actionStatus
            ? _resourceService.GetResourceString("SuccessActionResult")
            : _resourceService.GetResourceString("ErrorActionResult");
        await _menuService.ViewPrivacyMenu(botClient, update, statusMessage);
    }
}

public class UserEnablePermanentContentSpoilerCommand : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService;
    private readonly IPrivacySettingsSetter _privacySettingsSetter;
    private readonly IResourceService _resourceService;
    private readonly IUserGetter _userGetter;

    public UserEnablePermanentContentSpoilerCommand(
        IPrivacySettingsSetter privacySettingsSetter,
        IUserGetter userGetter,
        IUserMenuService menuService,
        IResourceService resourceService)
    {
        _privacySettingsSetter = privacySettingsSetter;
        _userGetter = userGetter;
        _menuService = menuService;
        _resourceService = resourceService;
    }

    public string Name => "user_allow_content_forwarding";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id);
        bool actionStatus = await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING, "disallow_content_forwarding", true, "always");
        string statusMessage = actionStatus
            ? _resourceService.GetResourceString("SuccessActionResult")
            : _resourceService.GetResourceString("ErrorActionResult");
        await _menuService.ViewPrivacyMenu(botClient, update, statusMessage);
    }
}

public class UserUpdateSiteStopListCommand(IUserMenuService menuService) : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService = menuService;
    public string Name => "user_update_site_stop_list";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _menuService.ViewSiteFilterMenu(botClient, update);
    }
}

public class UserSetSiteStopListSettingsCommand(IUserMenuService menuService) : IBotCallbackQueryHandlers
{
    private readonly IUserMenuService _menuService = menuService;
    public string Name => "user_set_site_stop_list_settings";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _menuService.ViewSiteFilterSettingsMenu(botClient, update);
    }
}