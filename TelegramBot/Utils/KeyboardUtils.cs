using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;
using TelegramMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class KeyboardUtils
{
    public static InlineKeyboardButton GetReturnButton(string callback = "main_menu", string? text = null)
    {
        text ??= Config.GetResourceString("BackButtonText");
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
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("YesButtonText"), acceptCallback),
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
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("MuteUserButtonText"), "mute_user"),
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("UnmuteUserButtonText"), "unmute_user"),
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
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("AddContactButtonText"), "add_contact"),
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("MyLinkButtonText"), "get_self_link"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ViewInboundInvitesButtonText"), "view_inbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ViewOutboundInvitesButtonText"), "view_outbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ViewAllContactsButtonText"), "view_contacts"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("UsersGroupButtonText"), "show_groups"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("BehindTheScenesButtonText"), "whos_the_genius")
                        }
                    });
        return Utils.SendMessage(botClient, update, inlineKeyboard, cancellationToken, text);
    }
}