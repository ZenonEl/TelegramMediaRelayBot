using Telegram.Bot;
using Telegram.Bot.Types;
using DataBase;
using TikTokMediaRelayBot;
using System.Globalization;

namespace MediaTelegramBot.Utils;

public static class CallbackQueryMenuUtils
{
    public static Task GetSelfLink(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string link = DBforGetters.GetSelfLink(update.CallbackQuery!.Message!.Chat.Id);
        return Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(Config.resourceManager.GetString("YourLink", CultureInfo.CurrentUICulture)!, link));
    }

    public static async Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string text = Config.resourceManager.GetString("YourInboundInvitations", CultureInfo.CurrentUICulture)!;
        await Utils.SendMessage(botClient, update, KeyboardUtils.GetInboundsKeyboardMarkup(update), cancellationToken, text);
    }

    public static Task AcceptInboundInvite(Update update)
    {
        DBforInbounds.SetContactStatus(long.Parse(update.CallbackQuery!.Data!.Split(':')[1]), update.CallbackQuery.Message!.Chat.Id, "accepted");
        return Task.CompletedTask;
    }

    public static Task WhosTheGenius(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string text = Config.resourceManager.GetString("WhosTheGeniusText", CultureInfo.CurrentUICulture)!;
        return Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, text);
    }
}