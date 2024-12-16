using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Menu;
using MediaTelegramBot.Utils;
using Serilog;
using System.Resources;
using System.Globalization;
using TikTokMediaRelayBot;

namespace MediaTelegramBot;

public class PrivateUpdateHandler
{

    public static async Task ProcessMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        string messageText = update.Message!.Text!;
        string link;
        string text = "";

        int newLineIndex = messageText.IndexOf('\n');

        if (newLineIndex != -1)
        {
            link = messageText.Substring(0, newLineIndex).Trim();
            text = messageText.Substring(newLineIndex + 1).Trim();
        }
        else
        {
            link = messageText.Trim();
        }

        if (Utils.Utils.IsLink(link))
        {
            await botClient.SendMessage(chatId, Config.resourceManager.GetString("WaitDownloadingVideo", CultureInfo.CurrentUICulture)!, cancellationToken: cancellationToken);
            _ = TelegramBot.HandleVideoRequest(botClient, link, chatId, caption: text);
        }
        else if (update.Message.Text == "/start")
        {
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        }
        else if (update.Message.Text == "/help")
        {
            string helpText = Config.resourceManager.GetString("HelpText", CultureInfo.CurrentUICulture)!;
            await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, helpText);
        }
        else
        {
            await botClient.SendMessage(update.Message.Chat.Id, Config.resourceManager.GetString("WhatShouldIDoWithThis", CultureInfo.CurrentUICulture)!, cancellationToken: cancellationToken);
        }
    }

    public static async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        var callbackQuery = update.CallbackQuery;

        switch (callbackQuery!.Data)
        {
            case "main_menu":
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
            case "add_contact":
                await Contacts.AddContact(botClient, update, cancellationToken);
                if (!TelegramBot.userStates.ContainsKey(chatId))
                {
                    TelegramBot.userStates[chatId] = new ProcessContactState();
                }
                break;
            case "get_self_link":
                await CallbackQueryMenuUtils.GetSelfLink(botClient, update, cancellationToken);
                break;
            case "view_inbound_invite_links":
                await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, cancellationToken);
                break;
            case "view_contacts":
                await Contacts.ViewContacts(botClient, update, cancellationToken);
                break;
            case "mute_user":
                await Contacts.MuteUserContact(botClient, update, cancellationToken, chatId);
                break;
            case "unmute_user":
                await Contacts.UnMuteUserContact(botClient, update, cancellationToken, chatId);
                break;
            case "whos_the_genius":
                await CallbackQueryMenuUtils.WhosTheGenius(botClient, update, cancellationToken);
                break;
            default:
                if (callbackQuery.Data!.StartsWith("user_accept_inbounds_invite:")) 
                {
                    await CallbackQueryMenuUtils.AcceptInboundInvite(update);
                }
                break;
        }
    }
}