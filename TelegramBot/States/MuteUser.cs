// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class MuteUserStateHandler : IStateHandler
{
    private readonly IContactAdder _contactAdder;
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;
    private readonly IUserTimeService _timeService;

    public string Name => "MuteUser";

    public MuteUserStateHandler(
        IContactAdder contactAdder,
        IUserGetter userGetter,
        IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker,
        IUserTimeService timeService)
    {
        _contactAdder = contactAdder;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
        _timeService = timeService;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        long chatId = _interactionService.GetChatId(update);

        if (await _stateBreaker.HandleStateBreak(botClient, update)) return StateResult.Complete();

        if (!stateData.Data.TryGetValue("MutedContactId", out object? contactIdObj) || contactIdObj is not int contactId)
        {
            return StateResult.Complete();
        }

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Выбор времени (Показываем Time Picker)
            // ========================================================================
            case 0:
                // Получаем имя контакта для красоты
                string contactName = _userGetter.GetUserNameByID(contactId) ?? $"ID {contactId}";

                // TODO Move: "Mute.TimePicker.Title"
                string pickerText = $"🔇 <b>На какое время заглушить {contactName}?</b>\n" +
                                    "Выберите вариант или напишите время вручную (например: <i>30m, 2h, 1d</i>).";

                // Клавиатура выбора времени
                // Callback format: "mute_time_select:{minutes}" (-1 = forever)
                InlineKeyboardMarkup timeKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        // TODO Move: "Time.15Minutes"
                        InlineKeyboardButton.WithCallbackData("15 мин", "mute_time_select:15"),
                        // TODO Move: "Time.1Hour"
                        InlineKeyboardButton.WithCallbackData("1 час", "mute_time_select:60"),
                        // TODO Move: "Time.8Hours"
                        InlineKeyboardButton.WithCallbackData("8 часов", "mute_time_select:480"),
                    },
                    new[]
                    {
                        // TODO Move: "Time.1Day"
                        InlineKeyboardButton.WithCallbackData("1 день", "mute_time_select:1440"),
                        // TODO Move: "Time.1Week"
                        InlineKeyboardButton.WithCallbackData("1 неделя", "mute_time_select:10080"),
                    },
                    new[]
                    {
                        // TODO Move: "Time.Forever"
                        InlineKeyboardButton.WithCallbackData("♾ Навсегда", "mute_time_select:-1")
                    },
                    new[] { KeyboardUtils.GetReturnButton("mute_contact") } // Возврат к списку контактов
                });

                await _interactionService.ReplyToUpdate(botClient, update, timeKeyboard, cancellationToken, pickerText);

                stateData.Step = 1;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Обработка выбора (Кнопка или Текст) -> Подтверждение
            // ========================================================================
            case 1:
                int minutes = 0;
                bool isForever = false;
                bool isValidInput = false;

                // Вариант А: Нажата кнопка
                if (update.CallbackQuery?.Data?.StartsWith("mute_time_select:") == true)
                {
                    string dataVal = update.CallbackQuery.Data.Split(':')[1];
                    minutes = int.Parse(dataVal);
                    if (minutes == -1) isForever = true;
                    isValidInput = true;
                }
                // Вариант Б: Введен текст вручную
                else if (!string.IsNullOrWhiteSpace(update.Message?.Text))
                {
                    (isValidInput, minutes, isForever) = ParseDuration(update.Message.Text);
                }
                // Вариант В: Нажата кнопка "Назад" (в меню контактов)
                else if (update.CallbackQuery?.Data == "mute_contact")
                {
                    return StateResult.Complete();
                }

                if (!isValidInput)
                {
                    // TODO Move: "Mute.Error.InvalidFormat"
                    await botClient.SendMessage(chatId, "⚠️ <b>Непонятный формат.</b>\nИспользуйте кнопки или пишите: 30m, 2h, 1d.",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                // 1. Вычисляем дату (ВСЕГДА UTC)
                DateTime? expirationDateUtc = isForever ? null : DateTime.UtcNow.AddMinutes(minutes);
                stateData.Data["ExpirationDate"] = expirationDateUtc;

                long currentChatId = _interactionService.GetChatId(update);
                int contactIdForDisplay = (int)contactIdObj; // для более явного типа

                // 2. Форматируем дату через сервис
                // ВАЖНО: Если мьют временный, показываем дату. Если навсегда, сервис вернет "навсегда".
                string expirationString = _timeService.FormatTimeForUser(_userGetter.GetUserIDbyTelegramID(currentChatId), expirationDateUtc);

                // 3. Формируем текст подтверждения
                string confirmName = _userGetter.GetUserNameByID(contactIdForDisplay) ?? "Unknown";

                // TODO Move: "Mute.Confirm.Prompt"
                string confirmText = $"❓ Вы уверены, что хотите заглушить <b>{confirmName}</b> до: <b>{expirationString}</b>?";

                await _interactionService.ReplyToUpdate(botClient, update,
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_mute"),
                    cancellationToken, confirmText);

                stateData.Step = 2;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 2: Финальное выполнение
            // ========================================================================
            case 2:
                if (update.CallbackQuery?.Data == "decline")
                {
                    // TODO Move: "Operation.Cancelled"
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, "❌ Операция отменена.");
                    return StateResult.Complete();
                }

                if (update.CallbackQuery?.Data != "accept_mute") return StateResult.Ignore();

                // Выполняем
                DateTime? finalExpiration = stateData.Data.ContainsKey("ExpirationDate") && stateData.Data["ExpirationDate"] != null
                    ? (DateTime)stateData.Data["ExpirationDate"]
                    : null;

                int myUserId = _userGetter.GetUserIDbyTelegramID(chatId);

                await _contactAdder.AddMutedContact(myUserId, contactId, finalExpiration);

                // TODO Move: "Mute.Success"
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, "✅ Контакт заглушен.");

                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }

    // --- Helpers ---

    private (bool Success, int Minutes, bool IsForever) ParseDuration(string input)
    {
        input = input.Trim().ToLowerInvariant();
        if (input == "навсегда" || input == "forever" || input == "inf") return (true, 0, true);

        // Простой парсер (1h, 30m, 1d)
        char lastChar = input[^1];
        if (char.IsDigit(lastChar))
        {
            // Если просто число, считаем минутами
            if (int.TryParse(input, out int m)) return (true, m, false);
        }
        else
        {
            string numberPart = input[..^1];
            if (int.TryParse(numberPart, out int val))
            {
                if (lastChar == 'm') return (true, val, false);
                if (lastChar == 'h') return (true, val * 60, false);
                if (lastChar == 'd') return (true, val * 60 * 24, false);
                if (lastChar == 'w') return (true, val * 60 * 24 * 7, false);
            }
        }
        return (false, 0, false);
    }

    private string FormatDuration(int minutes)
    {
        if (minutes < 60) return $"{minutes} мин";
        if (minutes < 1440) return $"{minutes / 60.0:F1} ч";
        return $"{minutes / 1440.0:F1} дн";
    }
}
