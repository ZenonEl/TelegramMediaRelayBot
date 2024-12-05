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
        return Task.CompletedTask;
    }

}

public static class KeyboardUtils
{
    public static InlineKeyboardButton GetReturnButton(string callback, string text = "Назад")
    {
        return InlineKeyboardButton.WithCallbackData(text, callback);
    }

    public static Task SendInlineKeyboardMenu(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Добавить контакт по ссылке", "add_contact"),
                            InlineKeyboardButton.WithCallbackData("Получить ссылку на себя", "get_self_link"),
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
        return botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Выберите опцию:",
                        replyMarkup: inlineKeyboard
                    );
    }

    public static Task AddContact(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {

    return botClient.SendMessage(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
                    );
    }

    public static Task ViewContacts(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        return botClient.SendMessage(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
                    );
    }

    public static Task WhosTheGenius(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        string text = @"Всем привет! 
Я, ZenonEl создатель этого мешка с кодом и алгоритмами.
Мой GitHub: https://github.com/ZenonEl/

Идея создания этого бота возникла у меня, когда в очередной раз мои знакомые были вынуждены присылать видео с Тик Тока (которым я не пользуюсь) вручную (скачивая его и отправляя... потом удаляя видео с телефона...).
И я подумал, что было бы здорово облегчить все эти монотонные действия для них и решил создать этого самого бота
Теперь благодаря ему я могу запустить простого бота хоть у себя на ПК. Выставить список контактов от кого я хочу получать видосики и всё, облегчил жизнь и себе, и своим знакомым.
Удобно!

Приятного пользования!";
        return botClient.EditMessageText(
                        chatId: callbackQuery.Message.Chat.Id,
                        messageId: callbackQuery.Message.MessageId,
                        text: text
                    );
    }
}