using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramMediaRelayBot;

namespace MediaTelegramBot;

public class GroupUpdateHandler
{
    public static async Task HandleGroupUpdate(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string messageText = update.Message!.Text!;
        if (messageText.Contains("/link"))
        {
            Log.Information(messageText);
            string link;
            string text = "";

            string trimmedMessage = messageText[5..].Trim();

            int separatorIndex = trimmedMessage.IndexOfAny([' ', '\n']);

            if (separatorIndex != -1)
            {
                link = trimmedMessage[..separatorIndex].Trim();
                text = trimmedMessage[separatorIndex..].Trim();
            }
            else
            {
                link = trimmedMessage.Trim();
            }

            if (Utils.Utils.IsLink(link))
            {
                Message statusMessage = await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);
                _ = TelegramBot.HandleVideoRequest(botClient, link, update.Message.Chat.Id, statusMessage: statusMessage, groupChat: true, caption: text);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("InvalidLinkFormat"), cancellationToken: cancellationToken);
            }
        }
        else if (messageText == "/help")
        {
            string text = Config.GetResourceString("GroupHelpText");
            await botClient.SendMessage(update.Message.Chat.Id, text, cancellationToken: cancellationToken);
        }
    }
}