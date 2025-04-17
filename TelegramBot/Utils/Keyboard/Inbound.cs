// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;


namespace TelegramMediaRelayBot.TelegramBot.Utils ;

public static class InBoundKB
{
    public static InlineKeyboardMarkup GetInBoundActionsKeyboardMarkup(string userId, string callbackData)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("AcceptButtonText"), $"user_accept_inbounds_invite:{userId}"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DeclineButtonText"), $"user_decline_inbounds_invite:{userId}"),
                        },
                        new[]
                        {
                            KeyboardUtils.GetReturnButton(callbackData)
                        },
                    });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetInboundsKeyboardMarkup(Update update)
    {
        var buttonDataList = DBforInbounds.GetInboundsButtonData(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));

        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (var buttonData in buttonDataList)
        {
            var button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }
}
