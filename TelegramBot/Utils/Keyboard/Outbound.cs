// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

public class OutBoundKB
{
    private static readonly System.Resources.ResourceManager _resourceManager =
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);

    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }

    public static async Task<InlineKeyboardMarkup> GetOutboundKeyboardMarkup(long userId, IOutboundDBGetter outboundDBGetter, IUserGetter userGetter)
    {
        List<Database.ButtonData> buttonDataList = await outboundDBGetter.GetOutboundButtonDataAsync(userGetter.GetUserIDbyTelegramID(userId));

        List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (Database.ButtonData buttonData in buttonDataList)
        {
            InlineKeyboardButton button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    public static InlineKeyboardMarkup GetOutboundActionsKeyboardMarkup(string userId)
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("DeclineButtonText"), $"revoke_outbound_invite:{userId}"),
                        },
                        new[]
                        {
                            KeyboardUtils.GetReturnButton()
                        },
                    });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetOutBoundActionsKeyboardMarkup(string userId, string callbackData)
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("YesButtonText"), $"user_accept_revoke_outbound_invite:{userId}"),
                        },
                        new[]
                        {
                            KeyboardUtils.GetReturnButton(callbackData)
                        },
                    });
        return inlineKeyboard;
    }
}
