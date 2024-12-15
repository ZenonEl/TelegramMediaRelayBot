using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;
using TikTokMediaRelayBot;

namespace MediaTelegramBot.Utils;

public static class KeyboardUtils
{
    public static InlineKeyboardButton GetReturnButton(string callback = "main_menu", string text = null)
    {
        text ??= Config.resourceManager.GetString("BackButtonText", System.Globalization.CultureInfo.CurrentUICulture);
        return InlineKeyboardButton.WithCallbackData(text, callback);
    }

    public static InlineKeyboardMarkup GetReturnButtonMarkup(string callback = "main_menu", string text = null)
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

    public static async Task<InlineKeyboardMarkup> GetInboundsKeyboardMarkup(Update update)
    {
        var buttonDataList = DBforInbounds.GetButtonDataFromDatabase(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery.Message.Chat.Id));

        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (var buttonData in buttonDataList)
        {
            var button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    public static InlineKeyboardMarkup GetViewContactsKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("MuteUserButtonText", System.Globalization.CultureInfo.CurrentUICulture), "mute_user"),
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("UnmuteUserButtonText", System.Globalization.CultureInfo.CurrentUICulture), "unmute_user"),
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
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("AddContactButtonText", System.Globalization.CultureInfo.CurrentUICulture), "add_contact"),
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("MyLinkButtonText", System.Globalization.CultureInfo.CurrentUICulture), "get_self_link"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("ViewInboundInvitesButtonText", System.Globalization.CultureInfo.CurrentUICulture), "view_inbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("ViewOutboundInvitesButtonText", System.Globalization.CultureInfo.CurrentUICulture), "view_outbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("ViewAllContactsButtonText", System.Globalization.CultureInfo.CurrentUICulture), "view_contacts"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.resourceManager.GetString("BehindTheScenesButtonText", System.Globalization.CultureInfo.CurrentUICulture), "whos_the_genius")
                        }
                    });
        return Utils.SendMessage(botClient, update, inlineKeyboard, cancellationToken);
    }
}