// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot.Types.ReplyMarkups;


namespace TelegramMediaRelayBot.TelegramBot.Utils;

public static class ReplyKeyboardUtils
{

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

    public async static Task RemoveReplyMarkup(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var sentMessage = await botClient.SendMessage(chatId, "ㅤ", cancellationToken: cancellationToken, replyMarkup: new ReplyKeyboardRemove());
        await botClient.DeleteMessage(chatId, sentMessage.MessageId, cancellationToken);
    }
}