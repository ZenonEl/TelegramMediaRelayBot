// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;

namespace MediaTelegramBot
{
    public class ProcessUserSetDCSendState : IUserState
    {

        public UsersStandardState currentState;
        private List<int> targetIds = new();
        private bool isGroupIds;
        private int actingUserId;

        public ProcessUserSetDCSendState(bool isGroup)
        {
            currentState = UsersStandardState.ProcessAction;
            isGroupIds = isGroup;
        }

        public string GetCurrentState() => currentState.ToString();

        public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = Utils.Utils.GetIDfromUpdate(update);
            if (Utils.Utils.CheckNonZeroID(chatId)) return;

            if (!TelegramBot.userStates.TryGetValue(chatId, out IUserState? value) || value is not ProcessUserSetDCSendState userState)
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
                TelegramBot.userStates.Remove(chatId);
                await Utils.Utils.SendMessage(
                    botClient,
                    update,
                    UsersKB.GetUsersVideoSentUsersKeyboardMarkup(),
                    cancellationToken,
                    Config.GetResourceString("UsersVideoSentUsersMenuText")
                );
                return;
            }
            var messageText = update.Message?.Text;
            if (string.IsNullOrEmpty(messageText))
            {
                await botClient.SendMessage(chatId, "Invalid input");
                return;
            }

            var inputIds = messageText.Split(' ')
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();

            if (!inputIds.Any())
            {
                await botClient.SendMessage(chatId, "No valid IDs found");
                return;
            }

            userState.actingUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
            
            userState.targetIds = isGroupIds 
                ? ValidateGroupIds(userState.actingUserId, inputIds)
                : await ValidateUserIds(userState.actingUserId, inputIds);

            if (!userState.targetIds.Any())
            {
                await botClient.SendMessage(chatId, "No valid IDs found for your account");
                return;
            }

            var idsList = string.Join(", ", userState.targetIds);
            var message = $"{(isGroupIds ? "Groups" : "Users")} to process:\n{idsList}\n\nConfirm?";
            
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
                TelegramBot.userStates.Remove(chatId);
                await Utils.Utils.SendMessage(
                    botClient,
                    update,
                    UsersKB.GetUsersVideoSentUsersKeyboardMarkup(),
                    cancellationToken,
                    Config.GetResourceString("UsersVideoSentUsersMenuText")
                );
            }
        }

        private async Task HandleFinish(ITelegramBotClient botClient, Update update, long chatId,
            ProcessUserSetDCSendState userState, CancellationToken cancellationToken)
        {
            foreach (var id in userState.targetIds)
            {
                if (isGroupIds)
                {
                    // Логика для групп
                }
                else
                {
                    // Логика для пользователей
                }
            }

            TelegramBot.userStates.Remove(chatId);
            await Utils.Utils.SendMessage(
                botClient,
                update,
                UsersKB.GetUsersVideoSentUsersKeyboardMarkup(),
                cancellationToken,
                $"Successfully processed {userState.targetIds.Count} {(isGroupIds ? "groups" : "users")}"
            );
        }

        private async Task<List<int>> ValidateUserIds(int actingUserId, List<int> inputIds)
        {
            var allowedIds = await CoreDB.GetAllContactUserTGIds(actingUserId);
            return inputIds
                .Where(id => allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(id)))
                .ToList();
        }

        private List<int> ValidateGroupIds(int actingUserId, List<int> inputIds)
        {
            var userGroups = DBforGroups.GetGroupIDsByUserId(actingUserId);
            return inputIds
                .Where(id => userGroups.Any(g => g == id))
                .ToList();
        }
    }
}