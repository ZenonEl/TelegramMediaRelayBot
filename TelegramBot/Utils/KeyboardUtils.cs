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

public static class KeyboardUtils
{
    public static InlineKeyboardButton GetReturnButton(string callback = "main_menu", string? text = null)
    {
        text ??= Localization.Get("BackButtonText");
        return InlineKeyboardButton.WithCallbackData(text, callback);
    }

    public static InlineKeyboardMarkup GetReturnButtonMarkup(string callback = "main_menu", string? text = null)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            GetReturnButton(callback, text)
                        },
                    });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetConfirmForActionKeyboardMarkup(string acceptCallback = "accept", string denyCallback = "main_menu")
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("YesButtonText"), acceptCallback),
                        },
                        new[]
                        {
                            GetReturnButton(denyCallback)
                        },
                    });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetViewContactsKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("MuteUserButtonText"), "mute_contact"),
                            InlineKeyboardButton.WithCallbackData(Localization.Get("UnmuteUserButtonText"), "unmute_contact"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("EditContactNameButtonText"), "edit_contact_name"),
                            InlineKeyboardButton.WithCallbackData(Localization.Get("EditContactGroupButtonText"), "edit_contact_group"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("DeleteContactButtonText"), "delete_contact"),
                        },
                        new[]
                        {
                            GetReturnButton()
                        },
                    });
        return inlineKeyboard;
    }

    public static Task SendInlineKeyboardMenu(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, string? text = null)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("AddContactButtonText"), "add_contact"),
                            InlineKeyboardButton.WithCallbackData(Localization.Get("MyLinkButtonText"), "get_self_link"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("ViewInboundInvitesButtonText"), "view_inbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("ViewOutboundInvitesButtonText"), "view_outbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("ViewAllContactsButtonText"), "view_contacts"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("UsersGroupButtonText"), "show_groups"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("SettingsButtonText"), "show_settings"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Localization.Get("BehindTheScenesButtonText"), "whos_the_genius")
                        }
                    });
        return CommonUtilities.SendMessage(botClient, update, inlineKeyboard, cancellationToken, text);
    }

    public static InlineKeyboardMarkup GetVideoDistributionKeyboardMarkup(string? sessionId = null)
    {
        string suffix = sessionId != null ? $":{sessionId}" : "";
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(Localization.Get("SendToAllContactsButtonText"), $"send_to_all_contacts{suffix}"),
                                InlineKeyboardButton.WithCallbackData(Localization.Get("SendToDefaultGroupsButtonText"), $"send_to_default_groups{suffix}"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(Localization.Get("SendToSpecifiedGroupsButtonText"), $"send_to_specified_groups{suffix}"),
                                InlineKeyboardButton.WithCallbackData(Localization.Get("SendToSpecifiedUsersButtonText"), $"send_to_specified_users{suffix}"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(Localization.Get("SendOnlyToMeButtonText"), $"send_only_to_me{suffix}"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(Localization.Get("CancelButtonText"), $"cancel_media{suffix}"),
                            },
                        });
            return inlineKeyboard;
    }
}