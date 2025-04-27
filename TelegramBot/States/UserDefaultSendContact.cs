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
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot
{
    public class ProcessUserSetDCSendState : IUserState
    {

        public UsersStandardState currentState;
        private List<int> _targetIds = new();
        private bool _isGroupIds;
        private int _actingUserId;
        private readonly IContactGetter _contactGetterRepository;
        private readonly IDefaultAction _defaultAction;
        private readonly IDefaultActionGetter _defaultActionGetter;
        private readonly IUserGetter _userGetter;
        private readonly IGroupGetter _groupGetter;

        public ProcessUserSetDCSendState(
            bool isGroup,
            IContactGetter contactGetterRepository,
            IDefaultAction defaultAction,
            IDefaultActionGetter defaultActionGetter,
            IUserGetter userGetter,
            IGroupGetter groupGetter
            )
        {
            currentState = UsersStandardState.ProcessAction;
            _isGroupIds = isGroup;
            _contactGetterRepository = contactGetterRepository;
            _defaultAction = defaultAction;
            _defaultActionGetter = defaultActionGetter;
            _userGetter = userGetter;
            _groupGetter = groupGetter;
        }

        public string GetCurrentState() => currentState.ToString();

        public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = CommonUtilities.GetIDfromUpdate(update);
            if (CommonUtilities.CheckNonZeroID(chatId)) return;

            if (!TGBot.userStates.TryGetValue(chatId, out IUserState? value) || value is not ProcessUserSetDCSendState userState)
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
                    await HandleFinish(botClient, update, chatId, userState, cancellationToken);
                    break;
            }
        }

        private async Task HandleProcessAction(ITelegramBotClient botClient, Update update, long chatId, 
            ProcessUserSetDCSendState userState, CancellationToken cancellationToken)
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
                await botClient.SendMessage(chatId, "Invalid input", cancellationToken: cancellationToken);
                return;
            }

            var inputIds = messageText.Split(' ')
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();

            if (inputIds.Count == 0)
            {
                await botClient.SendMessage(chatId, "No valid IDs found", cancellationToken: cancellationToken);
                return;
            }

            userState._actingUserId = _userGetter.GetUserIDbyTelegramID(chatId);
            
            userState._targetIds = _isGroupIds 
                ? await ValidateGroupIds(userState._actingUserId, inputIds)
                : await ValidateUserIds(userState._actingUserId, inputIds);

            if (userState._targetIds.Count == 0)
            {
                await botClient.SendMessage(chatId, "No valid IDs found for your account", cancellationToken: cancellationToken);
                return;
            }

            var idsList = string.Join(", ", userState._targetIds);
            var message = $"{(_isGroupIds ? "Groups" : "Users")} to process:\n{idsList}\n\nConfirm?";
            
            await botClient.SendMessage(
                chatId,
                message,
                replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(),
                cancellationToken: cancellationToken);

            userState.currentState = UsersStandardState.ProcessData;
        }

        private async Task HandleConfirmation(ITelegramBotClient botClient, Update update, long chatId,
            ProcessUserSetDCSendState userState, CancellationToken cancellationToken)
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
                    UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
                    cancellationToken,
                    Config.GetResourceString("UsersVideoSentUsersMenuText")
                );
            }
        }

        private async Task HandleFinish(ITelegramBotClient botClient, Update update, long chatId,
            ProcessUserSetDCSendState userState, CancellationToken cancellationToken)
        {
            int userId = _userGetter.GetUserIDbyTelegramID(chatId);
            int actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);

            if (_isGroupIds)
            {
                _defaultAction.RemoveAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
            }
            else
            {
                _defaultAction.RemoveAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
            }

            foreach (var id in userState._targetIds)
            {
                if (_isGroupIds)
                {
                    _defaultAction.AddDefaultUsersActionTargets(userId, actionId, TargetTypes.GROUP, id);
                }
                else
                {
                    _defaultAction.AddDefaultUsersActionTargets(userId, actionId, TargetTypes.USER, id);
                }
            }

            TGBot.userStates.Remove(chatId);
            await CommonUtilities.SendMessage(
                botClient,
                update,
                UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
                cancellationToken,
                $"Successfully processed {userState._targetIds.Count} {(_isGroupIds ? "groups" : "users")}"
            );
        }

        private async Task<List<int>> ValidateUserIds(int actingUserId, List<int> inputIds)
        {
            var allowedIds = await _contactGetterRepository.GetAllContactUserTGIds(actingUserId);
            return inputIds
                .Where(id => allowedIds.Contains(_userGetter.GetTelegramIDbyUserID(id)))
                .ToList();
        }

        private async Task<List<int>> ValidateGroupIds(int actingUserId, List<int> inputIds)
        {
            var userGroups = await _groupGetter.GetGroupIDsByUserId(actingUserId);
            return inputIds
                .Where(id => userGroups.Any(g => g == id))
                .ToList();
        }
    }
}