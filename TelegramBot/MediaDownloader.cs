using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using TikTokMediaRelayBot;
using DataBase;

namespace MediaTelegramBot
{

    public class UserState
    {
        public ContactState State { get; set; }
    }

    class TelegramBot
    {
        private static ITelegramBotClient botClient;
        public static Dictionary<long, UserState> userStates = [];
        
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

        public static async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = Utils.GetIDfromUpdate(update);
            if (Utils.CheckNonZeroID(chatId)) return;

            var all_contact_states = ProcessContactState.GetAllStates();

            if (all_contact_states.Contains(userStates[chatId].State))
            {
                await ProcessContactState.ProcessState(botClient, update, cancellationToken);
            }
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = Utils.GetIDfromUpdate(update);
            if (Utils.CheckNonZeroID(chatId)) return;

            if (userStates.ContainsKey(chatId))
            {
                await ProcessState(botClient, update, cancellationToken);
                return;
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
                    DB.AddUser(update.Message.Chat.FirstName, update.Message.Chat.Id);
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
                Console.WriteLine($"Callback Query: {callbackQuery.Data}");
                switch (callbackQuery.Data)
                {
                    case "main_menu":
                        await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                        break;
                    case "add_contact":
                        await KeyboardUtils.AddContact(botClient, update, cancellationToken);
                        if (!userStates.ContainsKey(chatId))
                        {
                            userStates[chatId] = new UserState { State = ContactState.WaitingForLink };
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

    public static async Task SendVideoToTelegram(string videoUrl, long chatId, ITelegramBotClient botClient)
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

                    var message = await botClient.SendDocument(chatId, InputFile.FromStream(stream, "video.mp4"), caption: "Вот ваше видео!");
                    Console.WriteLine("Видео успешно отправлено в Telegram.");

                    // Отправка видео контактам
                    await SendVideoToContacts(message.Document.FileId, chatId, botClient);
                }
            }
            else
            {
                Console.WriteLine($"Ошибка при скачивании видео: {response.StatusCode}");
                await botClient.SendMessage(chatId, "Ошибка при скачивании видео.");
            }
        }
    }

    private static async Task SendVideoToContacts(string fileId, long userId, ITelegramBotClient botClient)
    {
        var contactUserIds = await GetContactUserIds(userId);

        foreach (var contactUserId in contactUserIds)
        {
            await botClient.SendDocument(contactUserId, InputFile.FromFileId(fileId), caption: "Вам отправили видео!");
            Console.WriteLine($"Видео успешно отправлено контакту с ID: {contactUserId}");
        }
    }

    }
}