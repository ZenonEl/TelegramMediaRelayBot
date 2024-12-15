using Telegram.Bot;
using Telegram.Bot.Types;


namespace MediaTelegramBot;


public class GroupUpdateHandler
{
    public static async Task HandleGroupUpdate(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (update.Message.Text.Contains("/link"))
        {
            string link = update.Message.Text.Replace("/link", "").Trim();
            update.Message.Text = link;

            if (Utils.Utils.IsLink(link))
            {
                string videoUrl = update.Message.Text;
                await botClient.SendMessage(update.Message.Chat.Id, "Подождите, идет скачивание видео...", cancellationToken: cancellationToken);
                _ = TelegramBot.HandleVideoRequest(botClient, videoUrl, update.Message.Chat.Id, true);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat.Id, "И что мне с этим делать? Не тот формат ссылки.", cancellationToken: cancellationToken);
            }
        }
        else if (update.Message.Text == "/help")
        {
            string text = @"Просто отправь мне команду: /link [ссылка_на_тт_видео]
PS: квадратные скобки не нужны :)";
            await botClient.SendMessage(update.Message.Chat.Id, text, cancellationToken: cancellationToken);
        }
    }
}