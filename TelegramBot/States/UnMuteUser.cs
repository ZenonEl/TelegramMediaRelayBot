// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class UnmuteUserStateHandler : IStateHandler
{
    private readonly IContactRemover _contactRemover;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "UnmuteUser";

    public UnmuteUserStateHandler(
        IContactRemover contactRemover,
        IContactGetter contactGetter,
        IUserGetter userGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _contactRemover = contactRemover;
        _contactGetter = contactGetter;
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

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Ожидание ID или ссылки от пользователя
            // ========================================================================
            case 0:
                var message = update.Message;
                if (message == null || string.IsNullOrWhiteSpace(message.Text))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                int contactId;
                var userId = _userGetter.GetUserIDbyTelegramID(chatId);
                var allowedIds = await _contactGetter.GetAllContactUserTGIds(userId);

                if (int.TryParse(message.Text, out int parsedId))
                {
                    contactId = parsedId;
                    var contactTelegramId = _userGetter.GetTelegramIDbyUserID(contactId);
                    var userName = _userGetter.GetUserNameByID(contactId);

                    if (string.IsNullOrEmpty(userName) || !allowedIds.Contains(contactTelegramId))
                    {
                        await botClient.SendMessage(chatId, _resourceService.GetResourceString("NoUserFoundByID"), cancellationToken: cancellationToken);
                        return StateResult.Complete();
                    }
                }
                else
                {
                    contactId = _contactGetter.GetContactIDByLink(message.Text);
                    var contactTelegramId = _userGetter.GetTelegramIDbyUserID(contactId);

                    if (contactId == -1 || !allowedIds.Contains(contactTelegramId))
                    {
                        await botClient.SendMessage(chatId, _resourceService.GetResourceString("NoUserFoundByLink"), cancellationToken: cancellationToken);
                        return StateResult.Complete();
                    }
                }

                stateData.Data["MutedByUserId"] = userId;
                stateData.Data["MutedContactId"] = contactId;
                
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("ConfirmDecision"), cancellationToken: cancellationToken);

                stateData.Step = 1;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения
            // ========================================================================
            case 1:
                if (!stateData.Data.TryGetValue("MutedContactId", out var contactIdObj)) return StateResult.Complete();

                var activeMuteTime = _contactGetter.GetActiveMuteTimeByContactID((int)contactIdObj);
                var text = string.Format(_resourceService.GetResourceString("UserInMute"), activeMuteTime);
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);

                stateData.Step = 2;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 2: Финальное подтверждение и выполнение
            // ========================================================================
            case 2:
                if (!stateData.Data.TryGetValue("MutedByUserId", out var mutedByObj) ||
                    !stateData.Data.TryGetValue("MutedContactId", out contactIdObj))
                {
                    return StateResult.Complete();
                }
                
                await _contactRemover.RemoveMutedContact((int)mutedByObj, (int)contactIdObj);
                await _stateBreaker.AlertAndShowMenu(botClient, update, _resourceService.GetResourceString("UserUnmuted"));
                
                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}