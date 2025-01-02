using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Menu;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;


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
            link = messageText[..newLineIndex].Trim();
            text = messageText[(newLineIndex + 1)..].Trim();
        }
        else
        {
            link = messageText.Trim();
        }

        if (Utils.Utils.IsLink(link))
        {
            Message statusMessage = await botClient.SendMessage(chatId, Config.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);
            _ = TelegramBot.HandleVideoRequest(botClient, link, chatId, statusMessage, caption: text);
        }
        else if (update.Message.Text == "/start")
        {
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        }
        else if (update.Message.Text == "/help")
        {
            string helpText = Config.GetResourceString("HelpText");
            await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, helpText);
        }
        else
        {
            await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("WhatShouldIDoWithThis"), cancellationToken: cancellationToken);
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
                await Contacts.AddContact(botClient, update);
                if (!TelegramBot.userStates.ContainsKey(chatId))
                {
                    TelegramBot.userStates[chatId] = new ProcessContactState();
                }
                break;
            case "get_self_link":
                await CallbackQueryMenuUtils.GetSelfLink(botClient, update);
                break;
            case "view_inbound_invite_links":
                await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, chatId);
                break;
            case "view_outbound_invite_links":
                await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update);
                break;
            case "view_contacts":
                await Contacts.ViewContacts(botClient, update);
                break;
            case "show_groups":
                await Groups.ViewGroups(botClient, update, cancellationToken);
                break;
            case "mute_user":
                await Contacts.MuteUserContact(botClient, update, chatId);
                break;
            case "unmute_user":
                await Contacts.UnMuteUserContact(botClient, update, chatId);
                break;
            case "whos_the_genius":
                await CallbackQueryMenuUtils.WhosTheGenius(botClient, update);
                break;
            default:
                if (callbackQuery.Data!.StartsWith("user_show_outbound_invite:"))
                {
                    await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId);
                }
                break;
        }
    }
}