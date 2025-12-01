// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class OutboundInviteStateHandler : IStateHandler
{
    private readonly IContactRemover _contactRemover;
    private readonly IOutboundDBGetter _outboundDbGetter;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "OutboundInvite";

    public OutboundInviteStateHandler(
        IContactRemover contactRemover,
        IOutboundDBGetter outboundDbGetter,
        IUserGetter userGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _contactRemover = contactRemover;
        _outboundDbGetter = outboundDbGetter;
        _userGetter = userGetter;
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

        if (update.CallbackQuery?.Data == null) return StateResult.Ignore();
        var callbackData = update.CallbackQuery.Data;

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Ожидание выбора действия для конкретного приглашения
            // ========================================================================
            case 0:
                if (callbackData.StartsWith("revoke_outbound_invite:"))
                {
                    var userIdStr = callbackData.Split(':')[1];
                    stateData.Data["TargetUserIdStr"] = userIdStr; // Сохраняем ID цели

                    await _interactionService.ReplyToUpdate(botClient, update, OutBoundKB.GetOutBoundActionsKeyboardMarkup(userIdStr, "user_show_outbound_invite:" + chatId),
                                                cancellationToken, _resourceService.GetResourceString("DeclineOutBound"));

                    stateData.Step = 1; // Переходим к подтверждению
                    return StateResult.Continue();
                }
                // Если пришел любой другой callback на этом шаге, просто завершаем сценарий.
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                return StateResult.Complete();

            // ========================================================================
            // ШАГ 1: Ожидание финального подтверждения (отозвать или нет)
            // ========================================================================
            case 1:
                if (callbackData.StartsWith("user_show_outbound_invite:"))
                {
        string userId = update.CallbackQuery!.Data!.Split(':')[1];
        await _interactionService.ReplyToUpdate(botClient, update, OutBoundKB.GetOutboundActionsKeyboardMarkup(userId), cancellationToken, _resourceService.GetResourceString("OutboundInviteMenu"));
                }
                else if (callbackData.StartsWith("user_accept_revoke_outbound_invite:"))
                {
                    var userIdStr = callbackData.Split(':')[1];
                    var accepterId = _userGetter.GetUserIDbyTelegramID(long.Parse(userIdStr));
                    var senderId = _userGetter.GetUserIDbyTelegramID(chatId);
                    await _contactRemover.RemoveContactByStatus(senderId, accepterId, ContactsStatus.WAITING_FOR_ACCEPT);
                }

                // Любое действие на этом шаге завершает сценарий
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}
