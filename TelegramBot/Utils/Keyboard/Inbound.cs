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
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

public static class InBoundKB
{
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }
    
    public static InlineKeyboardMarkup GetInBoundActionsKeyboardMarkup(string userId, string callbackData)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("AcceptButtonText"), $"user_accept_inbounds_invite:{userId}"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("DeclineButtonText"), $"user_decline_inbounds_invite:{userId}"),
                        },
                        new[]
                        {
                            KeyboardUtils.GetReturnButton(callbackData)
                        },
                    });
        return inlineKeyboard;
    }

    public static async Task<InlineKeyboardMarkup> GetInboundsKeyboardMarkup(Update update, IInboundDBGetter inboundDBGetter, IUserGetter userGetter)
    {
        var buttonDataList = await inboundDBGetter.GetInboundsButtonDataAsync(userGetter.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));

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
