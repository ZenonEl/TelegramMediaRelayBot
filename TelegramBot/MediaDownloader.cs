using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using TikTokMediaRelayBot;

namespace MediaTelegramBot
{
    class TelegramBot
    {
        private static ITelegramBotClient botClient;

        static public async Task Start()
        {
            string telegramBotToken = Config.telegramBotToken;
            botClient = new TelegramBotClient(telegramBotToken);

            var me = await botClient.GetMe();
            Console.WriteLine($"Hello, I am {me.Id} ready and my name is {me.FirstName}.");

            var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            botClient.StartReceiving(UpdateHandler, Utils.ErrorHandler, receiverOptions, cancellationToken: cts.Token);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            cts.Cancel();
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message != null && update.Message.Text != null)
            {
                string pattern = @"^(https?:\/\/(www\.)?tiktok\.com\/@[\w.-]+\/video\/\d+|https?:\/\/vt\.tiktok\.com\/[\w.-]+\/?)$";
                Regex regex = new Regex(pattern);

                if (regex.IsMatch(update.Message.Text))
                {
                    string videoUrl = update.Message.Text;
                    await HandleVideoRequest(botClient, videoUrl, update.Message.Chat.Id);
                }
                else if (update.Message.Text == "/start")
                {
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(update.Message.Chat.Id, "И что мне с этим делать?", cancellationToken: cancellationToken);
                }
            }
            else if (update.CallbackQuery != null)
            {
                var callbackQuery = update.CallbackQuery;
                switch (callbackQuery.Data)
                {
                    case "add_contact":
                        await KeyboardUtils.AddContact(botClient, callbackQuery, cancellationToken);
                        break;
                    case "view_contacts":
                        await KeyboardUtils.ViewContacts(botClient, callbackQuery, cancellationToken);
                        break;
                    case "whos_the_genius":
                        await KeyboardUtils.WhosTheGenius(botClient, callbackQuery, cancellationToken);
                        break;
                }
                // string responseText = "Вы нажали кнопку!";

                // await botClient.SendMessage(
                //     chatId: callbackQuery.Message.Chat.Id,
                //     text: responseText,
                //     cancellationToken: cancellationToken
                // );

                // Подтверждаем обработку колбэка
                await botClient.AnswerCallbackQuery(callbackQuery.Id);
            }
        }

        private static async Task HandleVideoRequest(ITelegramBotClient botClient, string videoUrl, long chatId)
        {
            string inputId = "s_input";
            string downloadButtonClass = "btn-red";
            string finalDownloadButtonClass = "dl-success";
            string finalDownloadButtonID = "ConvertToVideo";
            int maxAttempts = 5;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                string downloadLink = VideoGet.GetDownloadLink(videoUrl, inputId, downloadButtonClass, finalDownloadButtonClass, finalDownloadButtonID);

                if (!string.IsNullOrEmpty(downloadLink))
                {
                    await SendVideoToTelegram(downloadLink, chatId);
                    return;
                }

                Console.WriteLine($"Attempt {attempt} failed. Retrying...");
                await Task.Delay(2000);
            }

            await botClient.SendMessage(chatId, "Не удалось получить ссылку на видео после 5 попыток.");
        }

        static async Task SendVideoToTelegram(string videoUrl, long chatId)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(videoUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var videoStream = await response.Content.ReadAsStreamAsync();
                    
                    using (var stream = new MemoryStream())
                    {
                        await videoStream.CopyToAsync(stream);
                        stream.Position = 0; 

                        await botClient.SendDocument(chatId, InputFile.FromStream(stream, "video.mp4"), caption: "Вот ваше видео!");
                        Console.WriteLine("Видео успешно отправлено в Telegram.");
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка при скачивании видео: {response.StatusCode}");
                    await botClient.SendMessage(chatId, "Ошибка при скачивании видео.");
                }
            }
        }
    }
}
