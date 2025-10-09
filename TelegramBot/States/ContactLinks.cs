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

/// <summary>
/// Handles contact link operations (keep/delete subset). Follows a unified 3-step flow:
/// ProcessAction -> ProcessData -> Finish. Uses inline keyboards, supports /start bailout.
/// </summary>
public class ProcessContactLinksState : IUserState
{

    public UsersStandardState currentState;
    private List<int> targetIds = new();
    private int actingUserId;
    private readonly bool isDeleteSelected;
    private readonly IContactRemover _contactRemover;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public ProcessContactLinksState(
        bool isDelete,
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository,
        IUserRepository userRepository,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService
        )
    {
        currentState = UsersStandardState.ProcessAction;
        isDeleteSelected = isDelete;
        _contactRemover = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
        _userRepository = userRepository;
        _userGetter = userGetter;
        _resourceService = resourceService;
    }

    public string GetCurrentState() => currentState.ToString();

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (!TGBot.StateManager.TryGet(chatId, out IUserState? value) || value is not ProcessContactLinksState userState)
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
            await FinishState(botClient, update, chatId, cancellationToken, _resourceService);
            return;
        }
        string? messageText = update.Message?.Text;
        if (string.IsNullOrEmpty(messageText))
        {
            await botClient.SendMessage(chatId, _resourceService.GetResourceString("State.ContactLinks.InvalidInput"), cancellationToken: cancellationToken);
            return;
        }

        var inputIds = messageText.Split(' ')
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .ToList();

        if (inputIds.Count == 0)
        {
            // Show available contacts before re-prompt
            var tgIds = await _contactGetterRepository.GetAllContactUserTGIds(_userGetter.GetUserIDbyTelegramID(chatId));
            var lines = new List<string>();
            foreach (var tg in tgIds)
            {
                int cid = _userGetter.GetUserIDbyTelegramID(tg);
                string uname = _userGetter.GetUserNameByTelegramID(tg);
                string link = _userGetter.GetUserSelfLink(tg);
                lines.Add(string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, link));
            }
            string header = _resourceService.GetResourceString("YourContacts");
            string body = lines.Count > 0 ? string.Join("\n", lines) : _resourceService.GetResourceString("NoUsersFound");
            string prompt = _resourceService.GetResourceString("State.ContactLinks.NoValidIds");
            await botClient.SendMessage(chatId, $"{header}\n{body}\n\n{prompt}", cancellationToken: cancellationToken);
            return;
        }

        userState.actingUserId = _userGetter.GetUserIDbyTelegramID(chatId);
        
        userState.targetIds = await ValidateUserIds(userState.actingUserId, inputIds);

        if (userState.targetIds.Count == 0)
        {
            await botClient.SendMessage(chatId, _resourceService.GetResourceString("State.ContactLinks.NoValidIdsForAccount"), cancellationToken: cancellationToken);
            return;
        }

        var idsList = string.Join(", ", userState.targetIds);
        var message = string.Format(_resourceService.GetResourceString("State.ContactLinks.ConfirmList"), idsList);
        
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
            await FinishState(botClient, update, chatId, cancellationToken, _resourceService);
        }
    }

    private async Task HandleFinish(ITelegramBotClient botClient, Update update, long chatId,
        ProcessContactLinksState userState, CancellationToken cancellationToken)
    {
        bool actionStatus;
        if (userState.isDeleteSelected)
        {
            actionStatus = await _contactRemover.RemoveUsersFromContacts(userState.actingUserId, userState.targetIds);
        }
        else 
        {
            actionStatus = await _contactRemover.RemoveAllContactsExcept(userState.actingUserId, userState.targetIds);
        }

        TGBot.StateManager.Remove(chatId);

        string statusMessage = actionStatus 
            ? _resourceService.GetResourceString("SuccessActionResult") 
            : _resourceService.GetResourceString("ErrorActionResult");

        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
            cancellationToken,
            _resourceService.GetResourceString("SelfLinkRefreshMenuText") + "\n\n" + statusMessage
        );
        _userRepository.ReCreateUserSelfLink(userState.actingUserId);

    }

    private static async Task FinishState(ITelegramBotClient botClient, Update update, long chatId, CancellationToken cancellationToken, TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        TGBot.StateManager.Remove(chatId);
        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
            cancellationToken,
            resourceService.GetResourceString("SelfLinkRefreshMenuText")
        );
    }

    private async Task<List<int>> ValidateUserIds(int actingUserId, List<int> inputIds)
    {
        var allowedIds = await _contactGetterRepository.GetAllContactUserTGIds(actingUserId);
        return inputIds
            .Where(id => allowedIds.Contains(_userGetter.GetTelegramIDbyUserID(id)))
            .ToList();
    }

}