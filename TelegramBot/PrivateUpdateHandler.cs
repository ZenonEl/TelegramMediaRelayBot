using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using TikTokMediaRelayBot;
using DataBase;

namespace MediaTelegramBot;


public class PrivateUpdateHandler
{
    public static async Task ProcessMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        string pattern = @"^(https?:\/\/(www\.)?tiktok\.com\/@[\w.-]+\/video\/\d+|https?:\/\/vt\.tiktok\.com\/[\w.-]+\/?)$";
        Regex regex = new Regex(pattern);

        if (regex.IsMatch(update.Message.Text))
        {
            string videoUrl = update.Message.Text;
            await botClient.SendMessage(update.Message.Chat.Id, "Подождите, идет скачивание видео...", cancellationToken: cancellationToken);
            await TelegramBot.HandleVideoRequest(botClient, videoUrl, update.Message.Chat.Id);
        }
        else if (update.Message.Text == "/start")
        {
            CoreDB.AddUser(update.Message.Chat.FirstName, update.Message.Chat.Id);
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        }
        else
        {
            await botClient.SendMessage(update.Message.Chat.Id, "И что мне с этим делать?", cancellationToken: cancellationToken);
        }
    }

    public static async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        var callbackQuery = update.CallbackQuery;
        Console.WriteLine($"Callback Query: {callbackQuery.Data}");
        switch (callbackQuery.Data)
        {
            case "main_menu":
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
            case "add_contact":
                await KeyboardUtils.AddContact(botClient, update, cancellationToken);
                if (!TelegramBot.userStates.ContainsKey(chatId))
                {
                    TelegramBot.userStates[chatId] = new UserState { State = ContactState.WaitingForLink };
                }
                break;
            case "get_self_link":
                await KeyboardUtils.GetSelfLink(botClient, update, cancellationToken);
                break;
            case "view_inbound_invite_links":
                await KeyboardUtils.ViewInboundInviteLinks(botClient, update, cancellationToken);
                break;
            case "view_contacts":
                await KeyboardUtils.ViewContacts(botClient, update, cancellationToken);
                break;
            case "whos_the_genius":
                await KeyboardUtils.WhosTheGenius(botClient, update, cancellationToken);
                break;
            default:
                if (callbackQuery.Data.StartsWith("user_accept_inbounds_invite:")) 
                {
                    await KeyboardUtils.AcceptInboundInvite(update);
                }
                break;
        }
    }
}