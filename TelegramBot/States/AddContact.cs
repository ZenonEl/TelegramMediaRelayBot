// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.


using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class AddContactStateHandler : IStateHandler
{
    private readonly IContactAdder _contactAdder;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    private readonly IStateBreakService _stateBreaker;
    private readonly ITelegramInteractionService _interactionService;

    public string Name => "AddContact";

    public AddContactStateHandler(
        IContactAdder contactAdder,
        IContactGetter contactGetter,
        IUserGetter userGetter,
        IResourceService resourceService,
        IStateBreakService stateBreaker,
        ITelegramInteractionService interactionService)
    {
        _contactAdder = contactAdder;
        _contactGetter = contactGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _stateBreaker = stateBreaker;
        _interactionService = interactionService;
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
            // ШАГ 0: Ожидание ссылки от пользователя
            // ========================================================================
            case 0:
                var link = update.Message?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(link))
                {
                    await _stateBreaker.AlertAndShowMenu(botClient, update, _resourceService.GetResourceString("InputErrorMessage"));
                    return StateResult.Complete();
                }

                int contactId = _contactGetter.GetContactIDByLink(link);
                if (contactId == -1)
                {
                    await _stateBreaker.AlertAndShowMenu(botClient, update, _resourceService.GetResourceString("NoUserFoundByLink"));
                    return StateResult.Complete();
                }

                // Проверка приватности (вся твоя логика остается)
                // ...

                // Сохраняем найденные данные в контейнер
                stateData.Data["Link"] = link;
                stateData.Data["FoundContactId"] = contactId;
                stateData.Data["FoundUserName"] = _userGetter.GetUserNameByID(contactId);

                var summary = $"{_resourceService.GetResourceString("LinkText")}: {link} \n{_resourceService.GetResourceString("NameText")}: {stateData.Data["FoundUserName"]}";
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("ConfirmAdditionText") + summary,
                    cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup());

                stateData.Step = 1; // Переходим на шаг подтверждения
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения (CallbackQuery 'accept'/'decline')
            // ========================================================================
            case 1:
                if (update.CallbackQuery?.Data == null)
                {
                    return StateResult.Ignore(); // Ждем только CallbackQuery
                }

                if (update.CallbackQuery.Data == "accept")
                {
                    // Достаем данные из контейнера
                    var originalLink = (string)stateData.Data["Link"];
                    var userTelegramId = chatId;
                    var contactTelegramId = _userGetter.GetTelegramIDbyUserID((int)stateData.Data["FoundContactId"]);
                    var senderName = _userGetter.GetUserNameByTelegramID(userTelegramId);

                    // Вызываем UoW-совместимый сервис
                    await _contactAdder.AddContact(userTelegramId, originalLink);
                    
                    // Отправляем уведомление
                    await botClient.SendMessage(contactTelegramId, 
                        string.Format(_resourceService.GetResourceString("UserWantsToAddYou"), senderName), 
                        cancellationToken: cancellationToken);
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("WaitForContactConfirmation"));
                }
                else // decline or any other callback
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                }
                
                // В любом случае сценарий завершен
                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}