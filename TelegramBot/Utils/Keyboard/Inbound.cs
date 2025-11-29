// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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
