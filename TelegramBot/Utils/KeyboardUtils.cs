using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;


namespace MediaTelegramBot.Utils;

public static class KeyboardUtils
{
    public static InlineKeyboardButton GetReturnButton(string callback = "main_menu", string text = "Назад")
    {
        return InlineKeyboardButton.WithCallbackData(text, callback);
    }

    public static InlineKeyboardMarkup GetReturnButtonMarkup(string callback = "main_menu", string text = "Назад")
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
                            InlineKeyboardButton.WithCallbackData("Замутить пользователя", "mute_user"),
                            InlineKeyboardButton.WithCallbackData("Размутить пользователя", "unmute_user"),
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
                            InlineKeyboardButton.WithCallbackData("Добавить контакт", "add_contact"),
                            InlineKeyboardButton.WithCallbackData("Моя ссылка", "get_self_link"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Обзор входящих запросов на добавления в мои контакты", "view_inbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Обзор моих заявок на добавления в контакты", "view_outbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Обзор всех моих контактов", "view_contacts"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Заглянуть за кулисы", "whos_the_genius")
                        }
                    });
        return Utils.SendMessage(botClient, update, inlineKeyboard, cancellationToken);
    }

}

