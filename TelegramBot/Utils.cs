using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;


namespace MediaTelegramBot;

public static class Utils
{
    public static Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error occurred: {exception.Message}");
        Console.WriteLine($"Stack trace: {exception.StackTrace}");

        // Дополнительная информация о внутренних исключениях, если они есть
        if (exception.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {exception.InnerException.Message}");
            Console.WriteLine($"Inner exception stack trace: {exception.InnerException.StackTrace}");
        }

        return Task.CompletedTask;
    }

    public static long GetIDfromUpdate(Update update)
    {
        if (update == null) return 0;
        if (update.Message != null)
        {
            return update.Message.Chat.Id;
        }
        else if (update.CallbackQuery != null)
        {
            return update.CallbackQuery.Message.Chat.Id;
        }
        return 0;
    }

    public static bool CheckNonZeroID(long id)
    {
        if (id == 0) return true;
        return false;
    }

    public static Task SendMessage(ITelegramBotClient botClient, Update update, InlineKeyboardMarkup inlineKeyboard
                                    ,CancellationToken cancellationToken, string text = "Выберите опцию:")
    {
        long chatId = GetIDfromUpdate(update);
        if (update.CallbackQuery != null) return botClient.EditMessageText(
                                                chatId: chatId,
                                                messageId: update.CallbackQuery.Message.MessageId,
                                                text: text,
                                                replyMarkup: inlineKeyboard,
                                                cancellationToken: cancellationToken,
                                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                                );
        if (update.Message != null) return botClient.SendMessage(
                                    chatId: chatId,
                                    text: text,
                                    replyMarkup: inlineKeyboard,
                                    cancellationToken: cancellationToken,
                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                                );
        return Task.CompletedTask;
    }
}

public static class KeyboardUtils
{
    public static InlineKeyboardButton GetReturnButton(string callback, string text = "Назад")
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

    public static InlineKeyboardMarkup GetInboundsKeyboardMarkup(Update update, CancellationToken cancellationToken)
    {
        var buttonDataList = DBforInbounds.GetButtonDataFromDatabase(DB.GetUserIDbyTelegramID(update.CallbackQuery.Message.Chat.Id));

        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (var buttonData in buttonDataList)
        {
            var button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    public static Task SendInlineKeyboardMenu(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Добавить контакт по ссылке", "add_contact"),
                            InlineKeyboardButton.WithCallbackData("Моя ссылка", "get_self_link"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Обзор входящих запросов на добавления в мои контакты.", "view_inbound_invite_links"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Обзор моих заявок на добавления в контакты.", "view_outbound_invite_links"),
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

    public static Task AddContact(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        return Utils.SendMessage(botClient, update, GetReturnButtonMarkup(), cancellationToken, "Укажите ссылку человека:");
    }

    public static Task GetSelfLink(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string link = DB.GetSelfLink(update.CallbackQuery.Message.Chat.Id);
        return Utils.SendMessage(botClient, update, GetReturnButtonMarkup(), cancellationToken, $"Ваша ссылка: <code>{link}</code>");
    }

    public static Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string text = $@"Ваши входящие приглашения:
(Нажимая на кнопку вы тем самым принимаете запрос на добавление в свои контакты)";
        return Utils.SendMessage(botClient, update, GetInboundsKeyboardMarkup(update, cancellationToken), cancellationToken, text);
    }

    public static Task AcceptInboundInvite(Update update)
    {
        DBforInbounds.SetContactStatus(long.Parse(update.CallbackQuery.Data.Split(':')[1]), update.CallbackQuery.Message.Chat.Id, "accepted");
        return Task.CompletedTask;
    }

    public static Task ViewContacts(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        return Utils.SendMessage(botClient, update, GetReturnButtonMarkup(), cancellationToken, "Ваши контакты:");
    }

    public static Task WhosTheGenius(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string text = @"Всем привет! 
Я, ZenonEl создатель этого мешка с кодом и алгоритмами.
Мой GitHub: https://github.com/ZenonEl/

Идея создания этого бота возникла у меня, когда в очередной раз мои знакомые были вынуждены присылать видео с Тик Тока (которым я не пользуюсь) вручную (скачивая его и отправляя... потом удаляя видео с телефона...).
В общем, и я подумал, что было бы здорово облегчить все эти монотонные действия для них и решил создать этого самого бота
Теперь благодаря ему я могу запустить простого бота хоть у себя на ПК. Выставить список контактов от кого я хочу получать видосики и всё, облегчил жизнь и себе, и своим знакомым.
Удобно!

Приятного пользования!";
        return Utils.SendMessage(botClient, update, GetReturnButtonMarkup(), cancellationToken, text);
    }

}

public static class ReplyKeyboardUtils
{

    public static ReplyKeyboardMarkup GetSingleButtonKeyboardMarkup(string text)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton(text)
        })
        {
            ResizeKeyboard = true
        };
        return replyKeyboard;
    }

    public async static Task RemoveReplyMarkup(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var sentMessage = await botClient.SendMessage(chatId, "ㅤ", cancellationToken: cancellationToken, replyMarkup: new ReplyKeyboardRemove()); 
        await botClient.DeleteMessage(chatId, sentMessage.MessageId, cancellationToken);
    }
    }