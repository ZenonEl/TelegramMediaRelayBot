// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ITelegramInteractionService
{
    long GetChatId(Update update);

    Task<Message?> ReplyToUpdate(
        ITelegramBotClient botClient,
        Update update,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default,
        string text = "ㅤ",
        int messageIdToEdit = 0,
        ParseMode parseMode = ParseMode.Html
    );
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

    public async Task<Message?> ReplyToUpdate(
        ITelegramBotClient botClient,
        Update update,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default,
        string text = "",
        int messageIdToEdit = 0,
        ParseMode parseMode = ParseMode.Html)
    {
        var chatId = GetChatId(update);
        if (chatId == 0) return null;

        int targetMessageId = 0;

        if (messageIdToEdit > 0)
        {
            targetMessageId = messageIdToEdit;
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            targetMessageId = update.CallbackQuery!.Message!.MessageId;
        }

        if (targetMessageId > 0)
        {
            try
            {
                return await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: targetMessageId,
                    text: text,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken,
                    parseMode: parseMode
                );
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("message is not modified"))
                {
                    return null;
                }
            }
        }
        try
        {
            return await botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken,
                parseMode: parseMode
            );
        }
        catch
        {
            return null;
        }
    }
}
