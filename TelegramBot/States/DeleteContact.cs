// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.


using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class RemoveContactsStateHandler : IStateHandler
{
    private readonly IContactRemover _contactRemover;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "RemoveContacts";

    public RemoveContactsStateHandler(
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
            // ШАГ 0: Ожидание списка ID контактов
            // ========================================================================
            case 0:
                var messageText = update.Message?.Text;
                if (string.IsNullOrEmpty(messageText))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                List<int> inputIds;
                try
                {
                    inputIds = messageText.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                }
                catch
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                if (inputIds.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                var actingUserId = _userGetter.GetUserIDbyTelegramID(chatId);
                var contactUserTGIds = await _contactGetter.GetAllContactUserTGIds(actingUserId);
                var preparedTargetUserTGIds = inputIds.Select(id => _userGetter.GetTelegramIDbyUserID(id)).ToList();
                
                var validTgIds = contactUserTGIds.Intersect(preparedTargetUserTGIds).ToList();

                if (!validTgIds.Any())
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("NoUsersFound"), cancellationToken: cancellationToken);
                    return StateResult.Continue(); // Остаемся в том же состоянии, ждем новый ввод
                }

                var contactUsersInfo = validTgIds.Select(tgId => {
                    int id = _userGetter.GetUserIDbyTelegramID(tgId);
                    string username = _userGetter.GetUserNameByTelegramID(tgId);
                    return string.Format(_resourceService.GetResourceString("ContactInfo"), id, username, "");
                }).ToList();

                var targetIdsToStore = validTgIds.Select(tgid => _userGetter.GetUserIDbyTelegramID(tgid)).ToList();
                stateData.Data["TargetIds"] = targetIdsToStore;

                var message = $"{_resourceService.GetResourceString("ConfirmRemovalMessage")}\n\n{string.Join("\n", contactUsersInfo)}";
                var keyboard = KeyboardUtils.GetConfirmForActionKeyboardMarkup("confirm_removal", "cancel_removal");
                
                await botClient.SendMessage(chatId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
                stateData.Step = 1; // Переходим на шаг подтверждения
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения (CallbackQuery)
            // ========================================================================
            case 1:
                if (update.CallbackQuery?.Data == null) return StateResult.Ignore();
                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);

                if (update.CallbackQuery.Data == "cancel_removal")
                {
                    // Пользователь отменил, возвращаемся к вводу ID
                    await botClient.EditMessageText(chatId, update.CallbackQuery.Message!.MessageId, 
                        _resourceService.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken);
                    stateData.Step = 0;
                    return StateResult.Continue();
                }

                if (update.CallbackQuery.Data == "confirm_removal")
                {
                    if (!stateData.Data.TryGetValue("TargetIds", out var targetIdsObj)) return StateResult.Complete();
                    
                    var currentUserId = _userGetter.GetUserIDbyTelegramID(chatId);
                    var targetIds = (List<int>)targetIdsObj;

                    var success = await _contactRemover.RemoveUsersFromContacts(currentUserId, targetIds);
                    var text = success ? _resourceService.GetResourceString("SuccessActionResult") : _resourceService.GetResourceString("ErrorActionResult");

                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), CancellationToken.None, text);
                    return StateResult.Complete();
                }
                
                return StateResult.Ignore();
        }

        return StateResult.Ignore();
    }
}