// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class DomainFilterStateHandler : IStateHandler
{
    private readonly IPrivacySettingsTargetsSetter _privacyTargetsSetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "DomainFilter";

    public DomainFilterStateHandler(
        IPrivacySettingsTargetsSetter privacyTargetsSetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _privacyTargetsSetter = privacyTargetsSetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update))
        {
            return StateResult.Complete();
        }

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Ожидание списка доменов от пользователя
            // ========================================================================
            case 0:
                var messageText = update.Message?.Text;
                if (string.IsNullOrEmpty(messageText))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                var inputDomains = messageText.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (inputDomains.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                var checkedDomains = ValidateDomains(inputDomains);
                if (checkedDomains.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                // Сохраняем проверенные домены в состояние
                stateData.Data["CheckedDomains"] = checkedDomains;

                var domainsListStr = string.Join(", ", checkedDomains);
                var message = $"{_resourceService.GetResourceString("ConfirmDecision")}:\n\n{domainsListStr}";

                await botClient.SendMessage(chatId, message,
                    replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);

                stateData.Step = 1; // Переходим на шаг подтверждения
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения (CallbackQuery)
            // ========================================================================
            case 1:
                if (update.CallbackQuery?.Data == null) return StateResult.Ignore();
                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);

                if (update.CallbackQuery.Data != "accept")
                {
                    await _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetSiteFilterKeyboardMarkup(),
                        cancellationToken, _resourceService.GetResourceString("SettingsMenuText"));
                    return StateResult.Complete();
                }

                // Пользователь нажал "accept", выполняем действие
                if (!stateData.Data.TryGetValue("IsRemove", out var isRemoveObj) ||
                    !stateData.Data.TryGetValue("PrivacyRuleId", out var ruleIdObj) ||
                    !stateData.Data.TryGetValue("UserId", out var userIdObj) ||
                    !stateData.Data.TryGetValue("CheckedDomains", out var domainsObj))
                {
                    return StateResult.Complete(); // Ошибка в данных состояния
                }

                var isRemove = (bool)isRemoveObj;
                var privacyRuleId = (int)ruleIdObj;
                var userId = (int)userIdObj;
                var domainsToProcess = (List<string>)domainsObj;

                if (isRemove)
                {
                    foreach (var domain in domainsToProcess)
                    {
                        await _privacyTargetsSetter.SetToRemovePrivacyRuleTarget(privacyRuleId, domain);
                    }
                }
                else
                {
                    foreach (var domain in domainsToProcess)
                    {
                        await _privacyTargetsSetter.SetPrivacyRuleTarget(userId, privacyRuleId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER, domain);
                    }
                }

                await _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetSiteFilterKeyboardMarkup(),
                    cancellationToken, _resourceService.GetResourceString("SuccessActionResult"));

                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }

    // Сохраняем твою оригинальную логику валидации доменов
    private List<string> ValidateDomains(List<string> inputDomains)
    {
        var checkedDomains = new HashSet<string>();
        foreach (var url in inputDomains)
        {
            var normalizedUrl = url;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                normalizedUrl = "https://" + url;
            }

            if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
            {
                checkedDomains.Add(uri.Host.ToLower());
            }
        }
        return checkedDomains.ToList();
    }
}
