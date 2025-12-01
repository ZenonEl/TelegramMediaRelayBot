// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class UnmuteUserStateHandler : IStateHandler
{
    private readonly IUiResourceService _uiResources;
    private readonly IStatesResourceService _statesResources;
    private readonly IErrorsResourceService _errorsResources;
    private readonly IContactRemover _contactRemover;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "UnmuteUser";

    public UnmuteUserStateHandler(
        IUiResourceService uiResources,
        IStatesResourceService statesResources,
        IErrorsResourceService errorsResources,
        IContactRemover contactRemover,
        IContactGetter contactGetter,
        IUserGetter userGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _uiResources = uiResources;
        _statesResources = statesResources;
        _errorsResources = errorsResources;
        _contactRemover = contactRemover;
        _contactGetter = contactGetter;
        _userGetter = userGetter;
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
            // ШАГ 0: Ожидание ID или ссылки от пользователя
            // ========================================================================
            case 0:
                Message? message = update.Message;
                if (message == null || string.IsNullOrWhiteSpace(message.Text))
                {
                    await botClient.SendMessage(chatId, _errorsResources.GetString("Error.Input.InvalidValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                int contactId;
                int userId = _userGetter.GetUserIDbyTelegramID(chatId);
                List<long> allowedIds = await _contactGetter.GetAllContactUserTGIds(userId);

                if (int.TryParse(message.Text, out int parsedId))
                {
                    contactId = parsedId;
                    long contactTelegramId = _userGetter.GetTelegramIDbyUserID(contactId);
                    string? userName = _userGetter.GetUserNameByID(contactId);

                    if (string.IsNullOrEmpty(userName) || !allowedIds.Contains(contactTelegramId))
                    {
                        await botClient.SendMessage(chatId, _errorsResources.GetString("Error.UserNotFoundById"), cancellationToken: cancellationToken);
                        return StateResult.Complete();
                    }
                }
                else
                {
                    contactId = _contactGetter.GetContactIDByLink(message.Text);
                    long contactTelegramId = _userGetter.GetTelegramIDbyUserID(contactId);

                    if (contactId == -1 || !allowedIds.Contains(contactTelegramId))
                    {
                        await botClient.SendMessage(chatId, _errorsResources.GetString("Error.UserNotFoundByLink"), cancellationToken: cancellationToken);
                        return StateResult.Complete();
                    }
                }

                stateData.Data["MutedByUserId"] = userId;
                stateData.Data["MutedContactId"] = contactId;

                await botClient.SendMessage(chatId, _uiResources.GetString("UI.ConfirmDecision"), cancellationToken: cancellationToken);

                stateData.Step = 1;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения
            // ========================================================================
            case 1:
                if (!stateData.Data.TryGetValue("MutedContactId", out object? contactIdObj)) return StateResult.Complete();

                string activeMuteTime = _contactGetter.GetActiveMuteTimeByContactID((int)contactIdObj);
                string text = string.Format(_statesResources.GetString("State.Unmute.Confirm.IsMuted"), activeMuteTime);
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);

                stateData.Step = 2;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 2: Финальное подтверждение и выполнение
            // ========================================================================
            case 2:
                if (!stateData.Data.TryGetValue("MutedByUserId", out object? mutedByObj) ||
                    !stateData.Data.TryGetValue("MutedContactId", out contactIdObj))
                {
                    return StateResult.Complete();
                }

                await _contactRemover.RemoveMutedContact((int)mutedByObj, (int)contactIdObj);
                await _stateBreaker.AlertAndShowMenu(botClient, update, _statesResources.GetString("State.Unmute.Success"));

                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}
