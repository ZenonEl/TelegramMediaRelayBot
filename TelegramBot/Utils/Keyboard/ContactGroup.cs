// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

public static class ContactGroup
{
    private static readonly System.Resources.ResourceManager _resourceManager =
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);

    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }

    public static InlineKeyboardMarkup GetContactGroupEditActionsKeyboardMarkup(int groupId)
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetResourceString("AddContactsButtonText"), $"user_add_contact_to_group:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetResourceString("RemoveContactsButtonText"), $"user_remove_contact_from_group:{groupId}"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
    }
}
