using Telegram.Bot;
using Telegram.Bot.Types;
using DataBase;
using TelegramMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class CallbackQueryMenuUtils
{
    public static CancellationToken cancellationToken = TelegramBot.cancellationToken;

    public static async Task GetSelfLink(ITelegramBotClient botClient, Update update)
    {
        string link = DBforGetters.GetSelfLink(update.CallbackQuery!.Message!.Chat.Id);
        User me = await botClient.GetMe();
        string startLink = $"\nhttps://t.me/{me.Username}?start={link}";
        await Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(Config.GetResourceString("YourLink") + startLink, link));
    }

    public static async Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update, long chatId)
    {
        string text = Config.GetResourceString("YourInboundInvitations");
        await Utils.SendMessage(botClient, update, InBoundKB.GetInboundsKeyboardMarkup(update), cancellationToken, text);
        TelegramBot.userStates[chatId] = new UserProcessInboundState();
    }

    public static async Task ViewOutboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        string text = Config.GetResourceString("YourOutboundInvitations");
        await Utils.SendMessage(botClient, update, OutBoundKB.GetOutboundKeyboardMarkup(Utils.GetIDfromUpdate(update)), cancellationToken, text);
    }

    public static async Task ShowOutboundInvite(ITelegramBotClient botClient, Update update, long chatId)
    {
        string userId = update.CallbackQuery!.Data!.Split(':')[1];
        await Utils.SendMessage(botClient, update, OutBoundKB.GetOutboundActionsKeyboardMarkup(userId), cancellationToken, Config.GetResourceString("OutboundInviteMenu"));
        TelegramBot.userStates[chatId] = new UserProcessOutboundState();
    }

    public static Task AcceptInboundInvite(Update update)
    {
        CoreDB.SetContactStatus(long.Parse(update.CallbackQuery!.Data!.Split(':')[1]), update.CallbackQuery.Message!.Chat.Id, DataBase.Types.ContactsStatus.ACCEPTED);
        return Task.CompletedTask;
    }

    public static Task DeclineInboundInvite(Update update)
    {
        CoreDB.SetContactStatus(long.Parse(update.CallbackQuery!.Data!.Split(':')[1]), update.CallbackQuery.Message!.Chat.Id, DataBase.Types.ContactsStatus.DECLINED);
        return Task.CompletedTask;
    }

    public static Task WhosTheGenius(ITelegramBotClient botClient, Update update)
    {
        string text = Config.GetResourceString("WhosTheGeniusText");
        return Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, text);
    }
}