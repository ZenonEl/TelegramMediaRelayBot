using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using TikTokMediaRelayBot;

namespace MediaTelegramBot
{
    public enum ContactState
    {
        WaitingForLink,
        WaitingForName,
        WaitingForConfirmation,
        Finish
    }
    public class UserState
    {
        public ContactState State { get; set; }
    }

    class TelegramBot
    {
        private static ITelegramBotClient botClient;
        private static Dictionary<long, UserState> _userStates = new Dictionary<long, UserState>();
        
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

        private static async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = 0;
            if (update.Message != null)
            {
                chatId = update.Message.Chat.Id;
                Console.WriteLine(_userStates.ContainsKey(chatId));
            }

            // Проверка, если пришел callback-запрос
            else if (update.CallbackQuery != null)
            {
                chatId = update.CallbackQuery.Message.Chat.Id;
            }
            switch (_userStates[chatId].State)
            {
                case ContactState.WaitingForLink:
                    await botClient.SendMessage(chatId, "Введите ссылку:");
                    _userStates[chatId].State = ContactState.WaitingForName;
                    break;

                case ContactState.WaitingForName:
                    // Логика обработки логина
                    await botClient.SendMessage(chatId, "Введите название для контакта:");
                    _userStates[chatId].State = ContactState.WaitingForConfirmation;
                    break;

                case ContactState.WaitingForConfirmation:
                    // Логика обработки пароля
                    await botClient.SendMessage(chatId, "Подтвердите добавление.");
                    _userStates[chatId].State = ContactState.Finish;
                    break;

                case ContactState.Finish:
                    await botClient.SendMessage(chatId, "Процесс завершен. Можете вернутся в меню.");
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    // Сброс состояния после завершения
                    _userStates.Remove(chatId);
                    break;
            }
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = 0;
            if (update == null) return;
            // Проверка, если пришло сообщение
            if (update.Message != null)
            {
                chatId = update.Message.Chat.Id;
                Console.WriteLine(_userStates.ContainsKey(chatId));
            }

            // Проверка, если пришел callback-запрос
            else if (update.CallbackQuery != null)
            {
                chatId = update.CallbackQuery.Message.Chat.Id;
            }

            

            if (_userStates.ContainsKey(chatId))
            {
                await ProcessState(botClient, update, cancellationToken);
            }

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
                        if (!_userStates.ContainsKey(update.CallbackQuery.Message.Chat.Id))
                        {
                            _userStates[update.CallbackQuery.Message.Chat.Id] = new UserState { State = ContactState.WaitingForLink };
                        }
                        Console.WriteLine(_userStates[update.CallbackQuery.Message.Chat.Id]);
                        break;
                    case "view_contacts":
                        await KeyboardUtils.ViewContacts(botClient, callbackQuery, cancellationToken);
                        break;
                    case "whos_the_genius":
                        await KeyboardUtils.WhosTheGenius(botClient, callbackQuery, cancellationToken);
                        break;
                }

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
