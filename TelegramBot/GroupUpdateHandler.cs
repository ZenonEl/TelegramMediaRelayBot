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
        if (!update.Message.Text.Contains("/link")) return;

        string cleanedText = update.Message.Text.Replace("/link", "").Trim();
        update.Message.Text = cleanedText;
        Console.WriteLine(update.Message.Text);

        string pattern = @"^(https?:\/\/(www\.)?tiktok\.com\/@[\w.-]+\/video\/\d+|https?:\/\/vt\.tiktok\.com\/[\w.-]+\/?)$";
        Regex regex = new Regex(pattern);

        if (regex.IsMatch(update.Message.Text))
        {
            string videoUrl = update.Message.Text;
            await botClient.SendMessage(update.Message.Chat.Id, "Подождите, идет скачивание видео...", cancellationToken: cancellationToken);
            await TelegramBot.HandleVideoRequest(botClient, videoUrl, update.Message.Chat.Id, true);
        }
    }
}