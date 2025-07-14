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


namespace TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

public static class ContactGroup
{

    public static InlineKeyboardMarkup GetContactGroupEditActionsKeyboardMarkup(int groupId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("AddContactsButtonText"), $"user_add_contact_to_group:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("RemoveContactsButtonText"), $"user_remove_contact_from_group:{groupId}"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
    }
}