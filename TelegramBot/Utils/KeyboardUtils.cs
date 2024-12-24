using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;
using TikTokMediaRelayBot;


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

    public static InlineKeyboardMarkup GetInboundsKeyboardMarkup(Update update)
    {
        var buttonDataList = DBforInbounds.GetInboundsButtonData(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));

        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (var buttonData in buttonDataList)
        {
            var button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    public static InlineKeyboardMarkup GetOutboundKeyboardMarkup(long userId)
    {
        var buttonDataList = DBforOutbound.GetOutboundButtonData(DBforGetters.GetUserIDbyTelegramID(userId));

        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (var buttonData in buttonDataList)
        {
            var button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    public static InlineKeyboardMarkup GetOutboundActionsKeyboardMarkup(string userId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DeclineButtonText"), $"revoke_outbound_invite:{userId}"),
                        },
                        new[]
                        {
                            GetReturnButton()
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

    public static Task SendInlineKeyboardMenu(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("BehindTheScenesButtonText"), "whos_the_genius")
                        }
                    });
        return Utils.SendMessage(botClient, update, inlineKeyboard, cancellationToken);
    }
}