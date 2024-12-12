using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using TikTokMediaRelayBot;
using DataBase;

namespace MediaTelegramBot;


public class GroupUpdateHandler
{
    public static async Task HandleGroupUpdate(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (update.Message.Text.Contains("/link"))
        {
            string cleanedText = update.Message.Text.Replace("/link", "").Trim();
            update.Message.Text = cleanedText;
            Console.WriteLine(update.Message.Text);

            string pattern = @"^(https?:\/\/(www\.)?tiktok\.com\/@[\w.-]+\/(video|photo)\/\d+|https?:\/\/vt\.tiktok\.com\/[\w.-]+\/?)(\?.*|\/.*)?$";

            Regex regex = new Regex(pattern);

            if (regex.IsMatch(update.Message.Text))
            {
                string videoUrl = update.Message.Text;
                await botClient.SendMessage(update.Message.Chat.Id, "Подождите, идет скачивание видео...", cancellationToken: cancellationToken);
                // _ = TelegramBot.HandleVideoRequest(botClient, videoUrl, update.Message.Chat.Id, true);
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