// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;


namespace TelegramMediaRelayBot;

public class ProcessContactLinksState : IUserState
{

    public UsersStandardState currentState;
    private List<int> targetIds = new();
    private int actingUserId;
    private readonly bool isDeleteSelected;
    private readonly IContactRemover _contactRemover;
    private readonly IContactGetter _contactGetterRepository;

    public ProcessContactLinksState(
        bool isDelete,
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository)
    {
        currentState = UsersStandardState.ProcessAction;
        isDeleteSelected = isDelete;
        _contactRemover = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
    }

    public string GetCurrentState() => currentState.ToString();

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (!TGBot.userStates.TryGetValue(chatId, out IUserState? value) || value is not ProcessContactLinksState userState)
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
        ProcessContactLinksState userState, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            await FinishState(botClient, update, chatId, cancellationToken);
            return;
        }
        string? messageText = update.Message?.Text;
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

        userState.actingUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
        
        userState.targetIds = await ValidateUserIds(userState.actingUserId, inputIds);

        if (userState.targetIds.Count == 0)
        {
            await botClient.SendMessage(chatId, "No valid IDs found for your account", cancellationToken: cancellationToken);
            return;
        }

        var idsList = string.Join(", ", userState.targetIds);
        var message = $" to process:\n{idsList}\n\nConfirm?";
        
        await botClient.SendMessage(
            chatId,
            message,
            replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(),
            cancellationToken: cancellationToken);

        userState.currentState = UsersStandardState.ProcessData;
    }

    private async Task HandleConfirmation(ITelegramBotClient botClient, Update update, long chatId,
        ProcessContactLinksState userState, CancellationToken cancellationToken)
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
            await FinishState(botClient, update, chatId, cancellationToken);
        }
    }

    private async Task HandleFinish(ITelegramBotClient botClient, Update update, long chatId,
        ProcessContactLinksState userState, CancellationToken cancellationToken)
    {
        bool actionStatus;
        if (userState.isDeleteSelected)
        {
            actionStatus = _contactRemover.RemoveUsersFromContacts(userState.actingUserId, userState.targetIds);
        }
        else 
        {
            actionStatus = _contactRemover.RemoveAllContactsExcept(userState.actingUserId, userState.targetIds);
        }

        TGBot.userStates.Remove(chatId);

        string statusMessage = actionStatus 
            ? Config.GetResourceString("SuccessActionResult") 
            : Config.GetResourceString("ErrorActionResult");

        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("SelfLinkRefreshMenuText") + "\n\n" + statusMessage
        );
        CoreDB.ReCreateUserSelfLink(userState.actingUserId);

    }

    private static async Task FinishState(ITelegramBotClient botClient, Update update, long chatId, CancellationToken cancellationToken)
    {
        TGBot.userStates.Remove(chatId);
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
            cancellationToken,
            Config.GetResourceString("SelfLinkRefreshMenuText")
        );
    }

    private async Task<List<int>> ValidateUserIds(int actingUserId, List<int> inputIds)
    {
        var allowedIds = await _contactGetterRepository.GetAllContactUserTGIds(actingUserId);
        return inputIds
            .Where(id => allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(id)))
            .ToList();
    }

}