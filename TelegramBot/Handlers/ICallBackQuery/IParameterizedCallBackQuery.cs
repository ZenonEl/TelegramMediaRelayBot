// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Menu;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;



public class ShowOutboundInviteCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRepository;
    private readonly IOutboundDBGetter _outboundDBGetter;
    private readonly IUserGetter _userGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public ShowOutboundInviteCommand(
        IContactRemover contactRepository,
        IOutboundDBGetter outboundDBGetter,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        _contactRepository = contactRepository;
        _outboundDBGetter = outboundDBGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public string Name => "user_show_outbound_invite:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;
        await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId, _contactRepository, _outboundDBGetter, _userGetter, _resourceService);
    }
}

public class SetAutoSendTimeCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_auto_send_video_time_to:";

    private readonly IDefaultActionSetter _defaultActionSetter;
    private readonly IUserGetter _userGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public SetAutoSendTimeCommand(
        IUserGetter userGetter,
        IDefaultActionSetter defaultActionSetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        _userGetter = userGetter;
        _defaultActionSetter = defaultActionSetter;
        _resourceService = resourceService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string callbackQueryData = update.CallbackQuery!.Data!.Split(':')[1];
        var chatId = update.CallbackQuery!.Message!.Chat.Id;
        bool result = Users.SetAutoSendVideoTimeToUser(chatId, callbackQueryData, _defaultActionSetter, _userGetter);

        var message = result 
            ? _resourceService.GetResourceString("AutoSendTimeChangedMessage") + callbackQueryData
            : _resourceService.GetResourceString("AutoSendTimeNotChangedMessage");

        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_set_auto_send_video_time"),
            ct,
            message
        );
    }
}

public class SetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    private readonly IContactGetter _contactGetterRepository;
    private readonly IDefaultAction _defaultAction;
    private readonly IDefaultActionSetter _defaultActionSetter;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public SetVideoSendUsersCommand(
        IContactGetter contactGetterRepository,
        IDefaultAction defaultAction,
        IDefaultActionSetter defaultActionSetter,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        IDefaultActionGetter defaultActionGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService
        )
    {
        _contactGetterRepository = contactGetterRepository;
        _defaultAction = defaultAction;
        _defaultActionSetter = defaultActionSetter;
        _userGetter = userGetter;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _defaultActionGetter = defaultActionGetter;
        _resourceService = resourceService;
    }

    public string Name => "user_set_video_send_users:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string action = update.CallbackQuery!.Data!.Split(':')[1];
        long chatId = update.CallbackQuery!.Message!.Chat.Id;

        List<string> extendActions = new List<string>
                                        {
                                            UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS,
                                            UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS,
                                        };

        if (extendActions.Contains(action))
        {
            await CommonUtilities.SendMessage(
                botClient,
                update,
                KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"),
                cancellationToken,
                _resourceService.GetResourceString("DefaultActionGetGroupOrUserIDs")
            );

            bool isGroup = false;
            if (action == UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS) isGroup = true;

            Users.SetDefaultActionToUser(chatId, action, _defaultActionSetter, _userGetter);
            TGBot.StateManager.Set(chatId, new ProcessUserSetDCSendState(
                isGroup,
                _contactGetterRepository,
                _defaultAction,
                _defaultActionGetter,
                _userGetter,
                _groupGetter,
                _resourceService));
            return;
        }


        bool result = Users.SetDefaultActionToUser(chatId, action, _defaultActionSetter, _userGetter);

        if (result)
        {
            await CommonUtilities.SendMessage(
                botClient,
                update,
                KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"),
                cancellationToken,
                _resourceService.GetResourceString("DefaultActionChangedMessage")
            );
            return;
        }
        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"),
            cancellationToken,
            _resourceService.GetResourceString("DefaultActionNotChangedMessage")
        );
    }
}

public class SetPrivacySiteFilterCommand : IBotCallbackQueryHandlers
{
    private readonly IPrivacySettingsSetter _privacySettingsSetter;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;
    private readonly IPrivacySettingsTargetsSetter _privacySettingsTargetsSetter;
    private readonly IPrivacySettingsTargetsGetter _privacySettingsTargetsGetter;
    private readonly IUserGetter _userGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;
    private bool isActive = false;

        public SetPrivacySiteFilterCommand(
        IPrivacySettingsSetter privacySettingsSetter,
        IPrivacySettingsGetter privacySettingsGetter,
        IPrivacySettingsTargetsSetter privacySettingsTargetsSetter,
        IPrivacySettingsTargetsGetter privacySettingsTargetsGetter,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService
    )
    {
        _privacySettingsSetter = privacySettingsSetter;
        _privacySettingsGetter = privacySettingsGetter;
        _privacySettingsTargetsSetter = privacySettingsTargetsSetter;
        _privacySettingsTargetsGetter = privacySettingsTargetsGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public string Name => "user_set_site_stop_list:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string callbackQueryData = update.CallbackQuery!.Data!.Split(':')[1];
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        bool switchResult = false;

        switch (callbackQueryData)
        {
            case "social":
                switchResult = await SwitchSocial(userId);
                break;
            case "nsfw":
                switchResult = await SwitchNSFW(userId);
                break;
            case "unified":
                switchResult = await SwitchUnified(userId);
                break;
            case "domains":
                switchResult = await SwitchDomains(userId);
                break;
            case "add_domains":
                await AddDomains(chatId, userId, update, botClient, ct);
                return;
            case "remove_domains":
                await RemoveDomains(chatId, userId, update, botClient, ct);
                return;
        }

        string text = switchResult ? _resourceService.GetResourceString("SuccessActionResult") : _resourceService.GetResourceString("ErrorActionResult");
        string actionText = !isActive ? _resourceService.GetResourceString("Enable") : _resourceService.GetResourceString("Disable");

        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_site_stop_list"),
            ct,
            $"{text} ({actionText})"
        );
    }

    private async Task<bool> SwitchSocial(int userId)
    {
        isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SOCIAL_SITE_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.SOCIAL_SITE_FILTER, PrivacyRuleAction.SOCIAL_FILTER, !isActive, "always");
    }

    private async Task<bool> SwitchNSFW(int userId)
    {
        isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.NSFW_SITE_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.NSFW_SITE_FILTER, PrivacyRuleAction.NSFW_FILTER, !isActive, "always");
    }

    private async Task<bool> SwitchUnified(int userId)
    {
        isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.UNIFIED_SITE_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.UNIFIED_SITE_FILTER, PrivacyRuleAction.UNIFIED_FILTER, !isActive, "always");
    }

    private async Task<bool> SwitchDomains(int userId)
    {
        isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER, PrivacyRuleAction.DOMAIN_FILTER, !isActive, "always");
    }

    private async Task AddDomains(long chatId, int userId, Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        int privacyRuleId = await _privacySettingsGetter.GetPrivacyRuleId(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
        
        if (privacyRuleId == 0) 
        {
            await CommonUtilities.SendMessage(
                botClient,
                update,
                KeyboardUtils.GetReturnButtonMarkup("user_update_site_stop_list"),
                ct,
                _resourceService.GetResourceString("DomainFilterNotEnabledError")
            );
            return;
        }

        await botClient.SendMessage(
            chatId, 
            _resourceService.GetResourceString("EnterDomainsToAddPrompt"), 
            cancellationToken: ct
        );

        TGBot.StateManager.Set(chatId, new ProcessUserAddDomainFilterState(
                privacyRuleId,
                _privacySettingsTargetsSetter,
                userId,
                false,
                _resourceService
            ));
    }

    private async Task RemoveDomains(long chatId, int userId, Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        bool isPrivacyTargetExist = await _privacySettingsTargetsGetter.CheckPrivacyRuleTargetExists(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
        
        if (!isPrivacyTargetExist) 
        {
            await CommonUtilities.SendMessage(
                botClient,
                update,
                KeyboardUtils.GetReturnButtonMarkup("user_update_site_stop_list"),
                ct,
                _resourceService.GetResourceString("NoDomainsAddedError")
            );
            return;
        }

        int privacyRuleId = await _privacySettingsGetter.GetPrivacyRuleId(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
        await botClient.SendMessage(
            chatId, 
            _resourceService.GetResourceString("EnterDomainsToRemovePrompt"), 
            cancellationToken: ct
        );

        TGBot.StateManager.Set(chatId, new ProcessUserAddDomainFilterState(
                privacyRuleId,
                _privacySettingsTargetsSetter,
                userId,
                true,
                _resourceService
            ));
    }
}