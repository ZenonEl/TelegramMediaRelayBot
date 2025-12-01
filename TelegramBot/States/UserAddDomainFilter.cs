// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

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
        long chatId = _interactionService.GetChatId(update);
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
                string? messageText = update.Message?.Text;
                if (string.IsNullOrEmpty(messageText))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                List<string> inputDomains = messageText.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (inputDomains.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                List<string> checkedDomains = ValidateDomains(inputDomains);
                if (checkedDomains.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                // Сохраняем проверенные домены в состояние
                stateData.Data["CheckedDomains"] = checkedDomains;

                string domainsListStr = string.Join(", ", checkedDomains);
                string message = $"{_resourceService.GetResourceString("ConfirmDecision")}:\n\n{domainsListStr}";

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
                if (!stateData.Data.TryGetValue("IsRemove", out object? isRemoveObj) ||
                    !stateData.Data.TryGetValue("PrivacyRuleId", out object? ruleIdObj) ||
                    !stateData.Data.TryGetValue("UserId", out object? userIdObj) ||
                    !stateData.Data.TryGetValue("CheckedDomains", out object? domainsObj))
                {
                    return StateResult.Complete(); // Ошибка в данных состояния
                }

                bool isRemove = (bool)isRemoveObj;
                int privacyRuleId = (int)ruleIdObj;
                int userId = (int)userIdObj;
                List<string> domainsToProcess = (List<string>)domainsObj;

                if (isRemove)
                {
                    foreach (string domain in domainsToProcess)
                    {
                        await _privacyTargetsSetter.SetToRemovePrivacyRuleTarget(privacyRuleId, domain);
                    }
                }
                else
                {
                    foreach (string domain in domainsToProcess)
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
        HashSet<string> checkedDomains = new HashSet<string>();
        foreach (string url in inputDomains)
        {
            string normalizedUrl = url;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                normalizedUrl = "https://" + url;
            }

            if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uri))
            {
                checkedDomains.Add(uri.Host.ToLower());
            }
        }
        return checkedDomains.ToList();
    }
}
