using Telegram.Bot;
using Telegram.Bot.Types;
using DataBase;


namespace MediaTelegramBot.Utils;

public static class CallbackQueryMenuUtils
{
    public static Task GetSelfLink(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string link = DBforGetters.GetSelfLink(update.CallbackQuery.Message.Chat.Id);
        return Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, $"Ваша ссылка: <code>{link}</code>");
    }

    public static async Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string text = $@"Ваши входящие приглашения:
(Нажимая на кнопку вы тем самым принимаете запрос на добавление в свои контакты)";
        await Utils.SendMessage(botClient, update, await KeyboardUtils.GetInboundsKeyboardMarkup(update), cancellationToken, text);
    }

    public static Task AcceptInboundInvite(Update update)
    {
        DBforInbounds.SetContactStatus(long.Parse(update.CallbackQuery.Data.Split(':')[1]), update.CallbackQuery.Message.Chat.Id, "accepted");
        return Task.CompletedTask;
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

Приятного пользования!

PS: В будущем планируется сделать бота универсальным реле. Где через конфиг можно будет выставить поддерживаемые сайты и то как боту с ними работать.";
        return Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, text);
    }

}
