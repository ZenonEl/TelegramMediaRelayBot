// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ITelegramInteractionService
{
    long GetChatId(Update update);
    Task ReplyToUpdate(ITelegramBotClient botClient, Update update, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default, string text = "ㅤ");
}

public class TelegramInteractionService : ITelegramInteractionService
{
    public long GetChatId(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message!.Chat.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message!.Chat.Id,
            _ => 0
        };
    }

    public Task ReplyToUpdate(ITelegramBotClient botClient, Update update, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default, string text = "")
    {
        var chatId = GetChatId(update);
        if (chatId == 0) return Task.CompletedTask;

        // Если это ответ на нажатие кнопки, редактируем исходное сообщение
        try
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                return botClient.EditMessageText(
                    chatId: chatId,
                    messageId: update.CallbackQuery!.Message!.MessageId,
                    text: text,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken,
                    parseMode: ParseMode.Html
                );
            }
        }
        catch
        {
            return botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Html
            );
        }
        
        // Если это обычное сообщение, отправляем новое
        if (update.Type == UpdateType.Message)
        {
            return botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Html
            );
        }

        return Task.CompletedTask;
    }
}