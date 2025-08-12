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
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }
    
    public static InlineKeyboardButton GetReturnButton(string callback = "main_menu", string? text = null)
    {
        text ??= GetResourceString("BackButtonText");
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

    public static InlineKeyboardMarkup GetConfirmForActionKeyboardMarkup(string acceptCallback = "accept", string denyCallback = "main_menu", int? messageId = null)
    {
        string suffix = messageId.HasValue ? $":{messageId.Value}" : string.Empty;
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("YesButtonText"), acceptCallback + suffix),
                        },
                        new[]
                        {
                            GetReturnButton(denyCallback + suffix)
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
                            InlineKeyboardButton.WithCallbackData(GetResourceString("MuteUserButtonText"), "mute_contact"),
                            InlineKeyboardButton.WithCallbackData(GetResourceString("UnmuteUserButtonText"), "unmute_contact"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("EditContactNameButtonText"), "edit_contact_name"),
                            InlineKeyboardButton.WithCallbackData(GetResourceString("EditContactGroupButtonText"), "edit_contact_group"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("DeleteContactButtonText"), "delete_contact"),
                        },
                        new[]
                        {
                            GetReturnButton()
                        },
                    });
        return inlineKeyboard;
    }

    public static Task SendInlineKeyboardMenu(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, string? text = null, int inboxNewCount = 0)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("AddContactButtonText"), "add_contact"),
                            InlineKeyboardButton.WithCallbackData(GetResourceString("MyLinkButtonText"), "get_self_link"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("ViewInboundInvitesButtonText"), "view_inbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("ViewOutboundInvitesButtonText"), "view_outbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("ViewAllContactsButtonText"), "view_contacts"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("UsersGroupButtonText"), "show_groups"),
                        },
                        new[] { InlineKeyboardButton.WithCallbackData(inboxNewCount > 0 ? $"{GetResourceString("OpenInboxButtonText")} ({inboxNewCount})" : GetResourceString("OpenInboxButtonText"), "open_inbox") },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("SettingsButtonText"), "show_settings"),
                            InlineKeyboardButton.WithCallbackData(GetResourceString("HelpButtonText"), "show_help"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(GetResourceString("BehindTheScenesButtonText"), "whos_the_genius")
                        }
                    });
        return CommonUtilities.SendMessage(botClient, update, inlineKeyboard, cancellationToken, text);
    }

    public static InlineKeyboardMarkup GetVideoDistributionKeyboardMarkup(int? messageId = null)
    {
        string suffix = messageId.HasValue ? $":{messageId.Value}" : string.Empty;
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(GetResourceString("SendToAllContactsButtonText"), "send_to_all_contacts" + suffix),
                                InlineKeyboardButton.WithCallbackData(GetResourceString("SendToDefaultGroupsButtonText"), "send_to_default_groups" + suffix),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(GetResourceString("SendToSpecifiedGroupsButtonText"), "send_to_specified_groups" + suffix),
                                InlineKeyboardButton.WithCallbackData(GetResourceString("SendToSpecifiedUsersButtonText"), "send_to_specified_users" + suffix),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(GetResourceString("SendOnlyToMeButtonText"), "send_only_to_me" + suffix),
                            },
                            new[]
                            {
                                GetReturnButton()
                            },
                        });
            return inlineKeyboard;
    }

    private static string TextOrDefault(string key, string fallback)
    {
        var value = GetResourceString(key);
        if (string.Equals(value, key, StringComparison.Ordinal)) return fallback;
        return value;
    }

    public static InlineKeyboardMarkup GetCancelKeyboardMarkup(int? messageId = null)
    {
        var cancelText = TextOrDefault("CancelButtonText", "Отменить");
        string suffix = messageId.HasValue ? $":{messageId.Value}" : string.Empty;
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData(cancelText, "cancel_download" + suffix) }
                    });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetConfirmWithCancelKeyboardMarkup(string acceptCallback = "accept", string denyCallback = "main_menu", int? messageId = null)
    {
        var cancelText = TextOrDefault("CancelButtonText", "Отменить");
        string suffix = messageId.HasValue ? $":{messageId.Value}" : string.Empty;
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData(GetResourceString("YesButtonText"), acceptCallback + suffix) },
                        new[] { InlineKeyboardButton.WithCallbackData(cancelText, "cancel_download" + suffix) },
                        new[] { GetReturnButton(denyCallback + suffix) }
                    });
        return inlineKeyboard;
    }
}