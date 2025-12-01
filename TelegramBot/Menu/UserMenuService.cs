// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IUserMenuService
{
    // Методы из класса Users
    Task ViewSettings(ITelegramBotClient botClient, Update update);
    Task ViewHelpMenu(ITelegramBotClient botClient, Update update);
    Task ViewDefaultActionsMenu(ITelegramBotClient botClient, Update update);
    Task ViewPrivacyMenu(ITelegramBotClient botClient, Update update, string statusMessage = "");
    Task ViewLinkPrivacyMenu(ITelegramBotClient botClient, Update update);
    Task ViewWhoCanFindMeByLinkMenu(ITelegramBotClient botClient, Update update);
    Task ViewPermanentContentSpoilerMenu(ITelegramBotClient botClient, Update update);
    Task ProcessViewPermanentContentSpoilerAction(ITelegramBotClient botClient, Update update);
    Task ViewVideoDefaultActionsMenu(ITelegramBotClient botClient, Update update, string? preface = null);
    Task ViewUsersVideoSentUsersActionsMenu(ITelegramBotClient botClient, Update update, string? preface = null);
    Task ViewAutoSendVideoTimeMenu(ITelegramBotClient botClient, Update update, string? preface = null);
    Task<bool> SetAutoSendVideoTimeToUser(long chatId, string time);
    Task<bool> SetDefaultActionToUser(long chatId, string action);
    Task ViewSiteFilterMenu(ITelegramBotClient botClient, Update update);
    Task ViewSiteFilterSettingsMenu(ITelegramBotClient botClient, Update update);

    // Методы из класса UsersDB
    void UpdateSelfLinkWithKeepSelectedContacts(Update update);
    void UpdateSelfLinkWithDeleteSelectedContacts(Update update);
    bool UpdateSelfLinkWithContacts(Update update);
    void UpdateSelfLinkWithNewContacts(Update update);
}

public class UserMenuService : IUserMenuService
{
    private readonly IHelpResourceService _helpResources;
    private readonly ISettingsResourceService _settingsResources;
    private readonly IUserStateManager _stateManager;
    private readonly IDefaultActionSetter _defaultActionSetter;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;
    private readonly IContactRemover _contactRemover;
    private readonly IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public UserMenuService(
        IHelpResourceService helpResources,
        ISettingsResourceService settingsResources,
        IUserStateManager stateManager,
        IDefaultActionSetter defaultActionSetter,
        IUserRepository userRepository,
        IUserGetter userGetter,
        IContactRemover contactRemover,
        IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _helpResources = helpResources;
        _settingsResources = settingsResources;
        _stateManager = stateManager;
        _defaultActionSetter = defaultActionSetter;
        _userRepository = userRepository;
        _userGetter = userGetter;
        _contactRemover = contactRemover;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    // --- Реализация методов из класса Users ---

    public Task ViewSettings(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersKB.GetSettingsKeyboardMarkup(),
            CancellationToken.None, _settingsResources.GetString("Settings.WhoCanFindMe.Nobody"));
    }

    public Task ViewHelpMenu(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup("main_menu"),
            CancellationToken.None, _helpResources.GetString("Help.Main"));
    }

    public Task ViewDefaultActionsMenu(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersDefaultActionsMenuKB.GetDefaultActionsMenuKeyboardMarkup(),
            CancellationToken.None, _settingsResources.GetString("Settings.DefaultActions.Title"));
    }

    public Task ViewPrivacyMenu(ITelegramBotClient botClient, Update update, string statusMessage = "")
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetPrivacyMenuKeyboardMarkup(),
            CancellationToken.None, _resourceService.GetResourceString("ChosePrivacyOptionMenuText") + "\n\n" + statusMessage);
    }

    public Task ViewLinkPrivacyMenu(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
            CancellationToken.None, _settingsResources.GetString("Settings.Link.Menu.Title"));
    }

    public Task ViewWhoCanFindMeByLinkMenu(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetWhoCanFindMeByLinkKeyboardMarkup(),
            CancellationToken.None, _settingsResources.GetString("Settings.SearchPrivacy.Title"));
    }

    public Task ViewPermanentContentSpoilerMenu(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetPermanentContentSpoilerKeyboardMarkup(),
            CancellationToken.None, _settingsResources.GetString("Settings.Forwarding.Description"));
    }

    public Task ProcessViewPermanentContentSpoilerAction(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetPermanentContentSpoilerKeyboardMarkup(),
            CancellationToken.None, _settingsResources.GetString("Settings.Forwarding.Description"));
    }

    public Task ViewVideoDefaultActionsMenu(ITelegramBotClient botClient, Update update, string? preface = null)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersDefaultActionsMenuKB.GetDefaultVideoDistributionKeyboardMarkup(),
            CancellationToken.None, (preface ?? string.Empty) + _settingsResources.GetString("Settings.DefaultVideoActions.Title"));
    }

    public Task ViewUsersVideoSentUsersActionsMenu(ITelegramBotClient botClient, Update update, string? preface = null)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
            CancellationToken.None, (preface ?? string.Empty) + _settingsResources.GetString("Settings.DefaultVideoActions.Summary.Header"));
    }

    public Task ViewAutoSendVideoTimeMenu(ITelegramBotClient botClient, Update update, string? preface = null)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersDefaultActionsMenuKB.GetUsersAutoSendVideoTimeKeyboardMarkup(),
            CancellationToken.None, (preface ?? string.Empty) + _settingsResources.GetString("Settings.AutoSendTime.Title"));
    }

    public Task<bool> SetAutoSendVideoTimeToUser(long chatId, string time)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        return _defaultActionSetter.SetAutoSendVideoConditionToUser(userId, time, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }

    public Task<bool> SetDefaultActionToUser(long chatId, string action)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        return _defaultActionSetter.SetAutoSendVideoActionToUser(userId, action, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
    }

    public Task ViewSiteFilterMenu(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetSiteFilterKeyboardMarkup(),
            CancellationToken.None, _resourceService.GetResourceString("SiteFilterMenuText"));
    }

    public Task ViewSiteFilterSettingsMenu(ITelegramBotClient botClient, Update update)
    {
        return _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetSiteFilterSettingsKeyboardMarkup(),
            CancellationToken.None, _resourceService.GetResourceString("SiteFilterMenuText"));
    }

    // --- Реализация методов из класса UsersDB ---

    public void UpdateSelfLinkWithKeepSelectedContacts(Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        UserStateData newState = new UserStateData
        {
            StateName = "ManageContacts",
            Step = 0,
            Data = new Dictionary<string, object> { { "IsDelete", false } }
        };
        _stateManager.Set(chatId, newState);
    }

    public void UpdateSelfLinkWithDeleteSelectedContacts(Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        UserStateData newState = new UserStateData
        {
            StateName = "ManageContacts",
            Step = 0,
            Data = new Dictionary<string, object> { { "IsDelete", true } }
        };
        _stateManager.Set(chatId, newState);
    }

    public bool UpdateSelfLinkWithContacts(Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        return _userRepository.ReCreateUserSelfLink(userId);
    }

    public void UpdateSelfLinkWithNewContacts(Update update)
    {
        int userId = _userGetter.GetUserIDbyTelegramID(_interactionService.GetChatId(update));
        _userRepository.ReCreateUserSelfLink(userId);
        _contactRemover.RemoveAllContacts(userId);
    }
}
