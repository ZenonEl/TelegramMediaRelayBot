using Telegram.Bot;
using Telegram.Bot.Types;
using TikTokMediaRelayBot;

namespace MediaTelegramBot;

public class GroupUpdateHandler
{
    public static async Task HandleGroupUpdate(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (update.Message!.Text!.Contains("/link"))
        {
            string link = update.Message.Text.Replace("/link", "").Trim();
            update.Message.Text = link;

            if (Utils.Utils.IsLink(link))
            {
                string videoUrl = update.Message.Text;
                Message statusMessage = await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);
                _ = TelegramBot.HandleVideoRequest(botClient, videoUrl, update.Message.Chat.Id, statusMessage, true);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("InvalidLinkFormat"), cancellationToken: cancellationToken);
            }
        }
        else if (update.Message.Text == "/help")
        {
            string text = Config.GetResourceString("GroupHelpText");
            await botClient.SendMessage(update.Message.Chat.Id, text, cancellationToken: cancellationToken);
        }
    }
}