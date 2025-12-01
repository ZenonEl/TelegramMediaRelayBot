// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class InboundInviteStateHandler : IStateHandler
{
    private readonly IUiResourceService _uiResources;
    private readonly IStatesResourceService _statesResources;
    private readonly IContactSetter _contactSetter;
    private readonly IContactRemover _contactRemover;
    private readonly IInboundDBGetter _inboundDbGetter;
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    private readonly IStateBreakService _stateBreaker;
    private readonly ITelegramInteractionService _interactionService;

    public string Name => "InboundInvite";

    public InboundInviteStateHandler(
        IUiResourceService uiResources,
        IStatesResourceService statesResources,
        IContactSetter contactSetter,
        IContactRemover contactRemover,
        IInboundDBGetter inboundDbGetter,
        IUserGetter userGetter,
        IResourceService resourceService,
        IStateBreakService stateBreaker,
        ITelegramInteractionService interactionService)
    {
        _uiResources = uiResources;
        _statesResources = statesResources;
        _contactSetter = contactSetter;
        _contactRemover = contactRemover;
        _inboundDbGetter = inboundDbGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _stateBreaker = stateBreaker;
        _interactionService = interactionService;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        long chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update))
        {
            return StateResult.Complete();
        }

        // Мы работаем только с CallbackQuery в этом состоянии
        if (update.CallbackQuery?.Data == null)
        {
            return StateResult.Ignore();
        }

        string callbackData = update.CallbackQuery.Data;

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Выбор конкретного приглашения из списка
            // ========================================================================
            case 0:
                if (callbackData.StartsWith("user_show_inbounds_invite:"))
                {
                    string userIdStr = callbackData.Split(':')[1];
                    // Сохраняем ID пользователя, с которым работаем, в данные состояния
                    stateData.Data["TargetUserId"] = userIdStr;

                    await _interactionService.ReplyToUpdate(botClient, update, InBoundKB.GetInBoundActionsKeyboardMarkup(userIdStr, "view_inbound_invite_links"),
                                                cancellationToken, _uiResources.GetString("UI.ChooseActionForRequest"));

                    // Переходим на следующий шаг
                    stateData.Step = 1;
                    return StateResult.Continue();
                }
                // Если пришел другой callback, завершаем сценарий
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                return StateResult.Complete();

            // ========================================================================
            // ШАГ 1: Ожидание действия (Принять/Отклонить)
            // ========================================================================
            case 1:
                if (!stateData.Data.TryGetValue("TargetUserId", out object? targetUserIdObj))
                {
                    Log.Warning("State 'InboundInvite' is missing 'TargetUserId' at Step 1.");
                    return StateResult.Complete();
                }
                string targetUserId = (string)targetUserIdObj;

                if (callbackData.StartsWith("user_accept_inbounds_invite:"))
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetConfirmForActionKeyboardMarkup($"accept_accept_invite:{targetUserId}", $"decline_accept_invite:{targetUserId}"),
                                                cancellationToken, _statesResources.GetString("State.InboundInvite.Confirm.Accept"));
                    stateData.Step = 2; // Переходим на шаг завершения
                    return StateResult.Continue();
                }
                else if (callbackData.StartsWith("user_decline_inbounds_invite:"))
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetConfirmForActionKeyboardMarkup($"accept_decline_invite:{targetUserId}", $"decline_decline_invite:{targetUserId}"),
                                                cancellationToken, _statesResources.GetString("State.InboundInvite.Confirm.Decline"));
                    stateData.Step = 2; // Переходим на шаг завершения
                    return StateResult.Continue();
                }
                // Если пользователь нажал что-то другое, просто выходим в главное меню
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                return StateResult.Complete();

            // ========================================================================
            // ШАГ 2: Финальное подтверждение и выполнение
            // ========================================================================
            case 2:
                if (callbackData.StartsWith("accept_accept_invite:"))
                {
                    // --- ЛОГИКА ИЗ AcceptInboundInvite ---
                    string senderIdStr = callbackData.Split(':')[1];
                    long senderTelegramId = long.Parse(senderIdStr);
                    long accepterTelegramId = update.CallbackQuery.Message!.Chat.Id;
                    await _contactSetter.SetContactStatus(senderTelegramId, accepterTelegramId, ContactsStatus.ACCEPTED);
                }
                else if (callbackData.StartsWith("accept_decline_invite:"))
                {
                    // --- ЛОГИКА ИЗ DeclineInboundInvite ---
                    string senderIdStr = callbackData.Split(':')[1];
                    int senderId = _userGetter.GetUserIDbyTelegramID(long.Parse(senderIdStr));
                    int accepterId = _userGetter.GetUserIDbyTelegramID(update.CallbackQuery.Message!.Chat.Id);
                    await _contactRemover.RemoveContactByStatus(senderId, accepterId, ContactsStatus.WAITING_FOR_ACCEPT);
                }

                // Вне зависимости от результата, завершаем сценарий и показываем главное меню
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}
