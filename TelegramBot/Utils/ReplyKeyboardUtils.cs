// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMediaRelayBot.TelegramBot.Utils;

/// <summary>
/// Utilities for working with reply keyboards (non-inline).
/// </summary>
public static class ReplyKeyboardUtils
{

    /// <summary>
    /// Builds a one-row reply keyboard with a single button.
    /// </summary>
    public static ReplyKeyboardMarkup GetSingleButtonKeyboardMarkup(string text)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton(text)
        })
        {
            ResizeKeyboard = true
        };
        return replyKeyboard;
    }

    /// <summary>
    /// Removes reply keyboard from chat by sending a dummy message and deleting it.
    /// </summary>
    public async static Task RemoveReplyMarkup(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var sentMessage = await botClient.SendMessage(chatId, "ㅤ", cancellationToken: cancellationToken, replyMarkup: new ReplyKeyboardRemove()).ConfigureAwait(false);
        await botClient.DeleteMessage(chatId, sentMessage.MessageId, cancellationToken).ConfigureAwait(false);
    }
}
