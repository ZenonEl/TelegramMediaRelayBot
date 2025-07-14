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
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database;

namespace TelegramMediaRelayBot
{
    public class ProcessUserAddDomainFilterState : IUserState
    {

        public UsersStandardState currentState;
        private readonly IPrivacySettingsTargetsSetter _privacySettingsTargetsSetter;
        private readonly int _privacyRuleId;
        private readonly int _userId;
        private List<string> _checkedDomains;
        private bool _isRemoveDomains;

        public ProcessUserAddDomainFilterState(
            int privacyRuleId,
            IPrivacySettingsTargetsSetter privacySettingsTargetsSetter,
            int userId,
            bool isRemoveDomains
            )
        {
            currentState = UsersStandardState.ProcessAction;
            _privacySettingsTargetsSetter = privacySettingsTargetsSetter;
            _privacyRuleId = privacyRuleId;
            _userId = userId;
            _isRemoveDomains = isRemoveDomains;
        }

        public string GetCurrentState() => currentState.ToString();

        public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = CommonUtilities.GetIDfromUpdate(update);
            if (CommonUtilities.CheckNonZeroID(chatId)) return;

            if (!TGBot.userStates.TryGetValue(chatId, out IUserState? value) || value is not ProcessUserAddDomainFilterState userState)
                return;

            switch (userState.currentState)
            {
                case UsersStandardState.ProcessAction:
                    await HandleProcessAction(botClient, update, chatId, userState, cancellationToken);
                    break;

                case UsersStandardState.ProcessData:
                    await HandleConfirmation(botClient, update, chatId, userState, cancellationToken);
                    break;

                case UsersStandardState.Finish:
                    await HandleFinish(botClient, update, chatId, cancellationToken);
                    break;
            }
        }

        private async Task HandleProcessAction(ITelegramBotClient botClient, Update update, long chatId, 
            ProcessUserAddDomainFilterState userState, CancellationToken cancellationToken)
        {
            if (update.CallbackQuery != null)
            {
                TGBot.userStates.Remove(chatId);
                await CommonUtilities.SendMessage(
                    botClient,
                    update,
                    UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
                    cancellationToken,
                    Config.GetResourceString("UsersVideoSentUsersMenuText")
                );
                return;
            }
            var messageText = update.Message?.Text;
            if (string.IsNullOrEmpty(messageText))
            {
                await botClient.SendMessage(chatId, Config.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                return;
            }

            List<string> inputDomains = messageText.Split(' ').ToList();

            if (inputDomains.Count == 0)
            {
                await botClient.SendMessage(chatId, Config.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                return;
            }

            _checkedDomains = ValidateDomains(inputDomains);

            if (_checkedDomains.Count == 0)
            {
                await botClient.SendMessage(chatId, Config.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken);
                return;
            }

            var domains = string.Join(", ", _checkedDomains);
            var message = $"{Config.GetResourceString("ConfirmDecision")}:\n\n{domains}";
            
            await botClient.SendMessage(
                chatId,
                message,
                replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(),
                cancellationToken: cancellationToken);

            userState.currentState = UsersStandardState.ProcessData;
        }

        private async Task HandleConfirmation(ITelegramBotClient botClient, Update update, long chatId,
            ProcessUserAddDomainFilterState userState, CancellationToken cancellationToken)
        {
            if (update.CallbackQuery == null) return;

            var callbackData = update.CallbackQuery.Data;
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);

            if (callbackData == "accept")
            {
                userState.currentState = UsersStandardState.Finish;
                await ProcessState(botClient, update, cancellationToken);
            }
            else
            {
                TGBot.userStates.Remove(chatId);
                await CommonUtilities.SendMessage(
                    botClient,
                    update,
                    UsersPrivacyMenuKB.GetSiteFilterKeyboardMarkup(),
                    cancellationToken,
                    Config.GetResourceString("SettingsMenuText")
                );
            }
        }

        private async Task HandleFinish(ITelegramBotClient botClient, Update update, long chatId,
                                        CancellationToken cancellationToken)
        {

            if (!_isRemoveDomains)
                foreach(string domain in _checkedDomains)
                {
                    await _privacySettingsTargetsSetter.SetPrivacyRuleTarget(_userId, _privacyRuleId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER, domain);
                }
            else
                foreach(string domain in _checkedDomains)
                {
                    await _privacySettingsTargetsSetter.SetToRemovePrivacyRuleTarget(_privacyRuleId, domain);
                }

            TGBot.userStates.Remove(chatId);
            await CommonUtilities.SendMessage(
                botClient,
                update,
                UsersPrivacyMenuKB.GetSiteFilterKeyboardMarkup(),
                cancellationToken,
                Config.GetResourceString("SuccessActionResult")
            );
        }

        private List<string> ValidateDomains(List<string> inputDomains)
        {
            HashSet<string> checkedDomains = new();
            foreach (string url in inputDomains)
            {
                string normalizedUrl = url;
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
}
