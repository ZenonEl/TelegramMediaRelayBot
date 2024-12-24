using Telegram.Bot;
using Telegram.Bot.Types;
using DataBase;
using TikTokMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class CallbackQueryMenuUtils
{
    public static CancellationToken cancellationToken = TelegramBot.cancellationToken;

    public static Task GetSelfLink(ITelegramBotClient botClient, Update update)
    {
        string link = DBforGetters.GetSelfLink(update.CallbackQuery!.Message!.Chat.Id);
        return Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(Config.GetResourceString("YourLink"), link));
    }

    public static async Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        string text = Config.GetResourceString("YourInboundInvitations");
        await Utils.SendMessage(botClient, update, KeyboardUtils.GetInboundsKeyboardMarkup(update), cancellationToken, text);
    }

    public static async Task ViewOutboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        string text = Config.GetResourceString("YourOutboundInvitations");
        await Utils.SendMessage(botClient, update, KeyboardUtils.GetOutboundKeyboardMarkup(Utils.GetIDfromUpdate(update)), cancellationToken, text);
    }

    public static async Task ShowOutboundInvite(ITelegramBotClient botClient, Update update, long chatId)
    {
        string userId = update.CallbackQuery!.Data!.Split(':')[1];
        await Utils.SendMessage(botClient, update, KeyboardUtils.GetOutboundActionsKeyboardMarkup(userId), cancellationToken, Config.GetResourceString("OutboundInviteMenu"));
        TelegramBot.userStates[chatId] = new ProcessUserProcessOutboundState();
    }

    public static Task AcceptInboundInvite(Update update)
    {
        DBforInbounds.SetContactStatus(long.Parse(update.CallbackQuery!.Data!.Split(':')[1]), update.CallbackQuery.Message!.Chat.Id, "accepted");
        return Task.CompletedTask;
    }

    public static Task WhosTheGenius(ITelegramBotClient botClient, Update update)
    {
        string text = Config.GetResourceString("WhosTheGeniusText");
        return Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, text);
    }
}