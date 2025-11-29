// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

// 1. Класс теперь stateless и зависит от сервисов, а не хранит их
public class MuteUserStateHandler : IStateHandler
{
    private readonly IContactAdder _contactAdder;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "MuteUser";

    public MuteUserStateHandler(
        IContactAdder contactAdder,
        IContactGetter contactGetter,
        IUserGetter userGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _contactAdder = contactAdder;
        _contactGetter = contactGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    // 2. Вся логика теперь в одном методе Process
    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var chatId = _interactionService.GetChatId(update);
        
        // Глобальная отмена через /start. Если сработало, завершаем сценарий.
        if (await _stateBreaker.HandleStateBreak(botClient, update))
        {
            return StateResult.Complete();
        }

        // 3. Используем switch по stateData.Step вместо enum
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
                    return StateResult.Continue(); // Продолжаем ждать ввод
                }

                int contactId;
                var userId = _userGetter.GetUserIDbyTelegramID(chatId);

                // Пытаемся распознать, ID это или ссылка
                if (int.TryParse(message.Text, out int parsedId))
                {
                    contactId = parsedId;
                    var contactTelegramId = _userGetter.GetTelegramIDbyUserID(contactId);
                    var userName = _userGetter.GetUserNameByID(contactId);
                    var allowedIds = await _contactGetter.GetAllContactUserTGIds(userId);

                    if (string.IsNullOrEmpty(userName) || !allowedIds.Contains(contactTelegramId))
                    {
                        await botClient.SendMessage(chatId, _resourceService.GetResourceString("NoUserFoundByID"), cancellationToken: cancellationToken);
                        return StateResult.Complete(); // Ошибка, завершаем
                    }
                }
                else
                {
                    contactId = _contactGetter.GetContactIDByLink(message.Text);
                    var allowedIds = await _contactGetter.GetAllContactUserTGIds(userId);
                    var contactTelegramId = _userGetter.GetTelegramIDbyUserID(contactId);

                    if (contactId == -1 || !allowedIds.Contains(contactTelegramId))
                    {
                        await botClient.SendMessage(chatId, _resourceService.GetResourceString("NoUserFoundByLink"), cancellationToken: cancellationToken);
                        return StateResult.Complete(); // Ошибка, завершаем
                    }
                }
                
                // Сохраняем найденные данные в наш контейнер
                stateData.Data["MutedByUserId"] = userId;
                stateData.Data["MutedContactId"] = contactId;

                await botClient.SendMessage(chatId, _resourceService.GetResourceString("ConfirmDecision"), cancellationToken: cancellationToken);
                
                // Переходим на следующий шаг
                stateData.Step = 1;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения (любое сообщение)
            // ========================================================================
            case 1:
                var text = _resourceService.GetResourceString("MuteTimeInstructions");
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
                
                stateData.Step = 2;
                return StateResult.Continue();
                
            // ========================================================================
            // ШАГ 2: Ожидание времени мьюта
            // ========================================================================
            case 2:
                var msg2 = update.Message;
                if (msg2 == null || string.IsNullOrWhiteSpace(msg2.Text))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidMuteTimeFormat"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                DateTime? expirationDate = null;
                // ... (здесь вся твоя логика парсинга времени из msg2.Text)
                // ...
                // Пример:
                if (int.TryParse(msg2.Text, out int seconds))
                {
                    expirationDate = DateTime.UtcNow.AddSeconds(seconds);
                }
                else if (msg2.Text.Equals("навсегда", StringComparison.OrdinalIgnoreCase))
                {
                    expirationDate = null;
                }
                else
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidMuteTimeFormat"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                // Сохраняем дату в контейнер
                stateData.Data["ExpirationDate"] = expirationDate;
                
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("ConfirmFinalDecision"), cancellationToken: cancellationToken);

                stateData.Step = 3;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 3: Финальное подтверждение и выполнение
            // ========================================================================
            case 3:
                // Достаем все данные из нашего контейнера
                var finalMutedBy = (int)stateData.Data["MutedByUserId"];
                var finalMutedContact = (int)stateData.Data["MutedContactId"];
                var finalExpiration = stateData.Data.ContainsKey("ExpirationDate") ? (DateTime?)stateData.Data["ExpirationDate"] : null;
                
                // Вызываем наш UoW-совместимый сервис
                await _contactAdder.AddMutedContact(finalMutedBy, finalMutedContact, finalExpiration);
                
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("MuteSet"), cancellationToken: cancellationToken);
                
                // Завершаем сценарий
                return StateResult.Complete();
        }

        return StateResult.Ignore(); // На всякий случай
    }
}