// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SetPrivacySiteFilterCommand : IBotCallbackQueryHandlers
{
    private readonly IUiResourceService _uiResources;
    private readonly IStatesResourceService _statesResources;
    private readonly IErrorsResourceService _errorsResources;
    private readonly IUserStateManager _stateManager;
    private readonly IPrivacySettingsSetter _privacySettingsSetter;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private bool _isActive;

    public SetPrivacySiteFilterCommand(
        IUiResourceService uiResources,
        IStatesResourceService statesResources,
        IErrorsResourceService errorsResources,
        IUserStateManager stateManager,
        IPrivacySettingsSetter privacySettingsSetter,
        IPrivacySettingsGetter privacySettingsGetter,
        IUserGetter userGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _uiResources = uiResources;
        _statesResources = statesResources;
        _errorsResources = errorsResources;
        _stateManager = stateManager;
        _privacySettingsSetter = privacySettingsSetter;
        _privacySettingsGetter = privacySettingsGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public string Name => "user_set_site_stop_list:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string callbackQueryData = update.CallbackQuery!.Data!.Split(':')[1];
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        // Логика для add_domains и remove_domains
        if (callbackQueryData == "add_domains" || callbackQueryData == "remove_domains")
        {
            await HandleDomainStateLaunch(botClient, update, chatId, userId, isRemove: callbackQueryData == "remove_domains", ct);
            return;
        }

        // Логика для переключателей
        bool switchResult = callbackQueryData switch
        {
            "social" => await SwitchSocial(userId),
            "nsfw" => await SwitchNSFW(userId),
            "unified" => await SwitchUnified(userId),
            "domains" => await SwitchDomains(userId),
            _ => false
        };

        string text = switchResult ? _uiResources.GetString("UI.Success") : _errorsResources.GetString("Error.ActionFailed");
        string actionText = !_isActive ? _uiResources.GetString("UI.Button.Enable") : _uiResources.GetString("UI.Button.Disable");

        await _interactionService.ReplyToUpdate(botClient, update,
            KeyboardUtils.GetReturnButtonMarkup("user_update_site_stop_list"), ct, $"{text} ({actionText})");
    }

    private async Task HandleDomainStateLaunch(ITelegramBotClient botClient, Update update, long chatId, int userId, bool isRemove, CancellationToken ct)
    {
        int privacyRuleId = await _privacySettingsGetter.GetPrivacyRuleId(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
        if (privacyRuleId == 0 && !isRemove)
        {
            // Обработка ошибки
            return;
        }

        UserStateData newState = new UserStateData
        {
            StateName = "DomainFilter",
            Step = 0,
            Data = new()
            {
                { "IsRemove", isRemove },
                { "PrivacyRuleId", privacyRuleId },
                { "UserId", userId }
            }
        };
        _stateManager.Set(chatId, newState);

        string prompt = isRemove
            ? _statesResources.GetString("State.RemoveDomain.Prompt.EnterDomains")
            : _statesResources.GetString("State.AddDomain.Prompt.EnterDomains");

        await botClient.SendMessage(chatId, prompt, cancellationToken: ct);
        await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct);
    }

    // Все методы Switch... остаются без изменений
    private async Task<bool> SwitchSocial(int userId)
    {
        bool isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SOCIAL_SITE_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.SOCIAL_SITE_FILTER, PrivacyRuleAction.SOCIAL_FILTER, !isActive, "always");
    }

    private async Task<bool> SwitchNSFW(int userId)
    {
        bool isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.NSFW_SITE_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.NSFW_SITE_FILTER, PrivacyRuleAction.NSFW_FILTER, !isActive, "always");
    }

    private async Task<bool> SwitchUnified(int userId)
    {
        bool isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.UNIFIED_SITE_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.UNIFIED_SITE_FILTER, PrivacyRuleAction.UNIFIED_FILTER, !isActive, "always");
    }

    private async Task<bool> SwitchDomains(int userId)
    {
        bool isActive = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
        return await _privacySettingsSetter.SetPrivacyRule(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER, PrivacyRuleAction.DOMAIN_FILTER, !isActive, "always");
    }
}
