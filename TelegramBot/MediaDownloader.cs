using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using TikTokMediaRelayBot;
using System.Text.RegularExpressions;
using DataBase;
using Serilog;

namespace MediaTelegramBot;


public class UserState
{
    public ContactState State { get; set; }
}

partial class TelegramBot
{
    private static ITelegramBotClient botClient;
    public static Dictionary<long, IUserState> userStates = new Dictionary<long, IUserState>();
    
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

        if (userStates[chatId] is IUserState userState)
        {
            string currentUserStatus = userState.GetCurrentState();
            Console.WriteLine($"Текущее состояние пользователя: {currentUserStatus}");

            await userState.ProcessState(botClient, update, cancellationToken);

        }
    }


    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.GetIDfromUpdate(update);
        if (Utils.CheckNonZeroID(chatId)) return;

        if (Utils.CheckPrivateChatType(update))
        {
            if (userStates.ContainsKey(chatId))
            {
                await ProcessState(botClient, update, cancellationToken);
                return;
            }

            if (update.Message != null && update.Message.Text != null)
            {
                CoreDB.AddUser(update.Message.Chat.FirstName, update.Message.Chat.Id);
                await PrivateUpdateHandler.ProcessMessage(botClient, update, cancellationToken, chatId);
            }
            else if (update.CallbackQuery != null)
            {
                await PrivateUpdateHandler.ProcessCallbackQuery(botClient, update, cancellationToken, chatId);
            }
        }
        else 
        {
            if (update.Message != null && update.Message.Text != null && update.Message.Text.Contains("/"))
            {
                await GroupUpdateHandler.HandleGroupUpdate(update, botClient, cancellationToken);
                return;
            }
        }
    }

    public static async Task HandleVideoRequest(ITelegramBotClient botClient, string videoUrl, long chatId, bool groupChat = false, string caption = "")
    {
        int maxAttempts = Config.maxAttempts;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            string downloadLink = await VideoGet.GetDownloadLink(videoUrl);
            if (!string.IsNullOrEmpty(downloadLink) && downloadLink != "#")
            {
                await SendVideoToTelegram(downloadLink, chatId, botClient, groupChat, caption);
                Console.WriteLine("Видео успешно получено.");
                return;
            }

            Console.WriteLine($"Attempt {attempt} failed. Retrying...");
        }

        await botClient.SendMessage(chatId, "Не удалось получить ссылку на видео после 5 попыток.");
    }


    public static async Task SendVideoToTelegram(string videoUrl, long chatId, ITelegramBotClient botClient, bool groupChat = false, string caption = "")
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

                    var message = await botClient.SendDocument(chatId, InputFile.FromStream(stream, "video.mp4"), caption: "Вот ваше видео! С текстом: \n\n" + caption);
                    Console.WriteLine("Видео успешно отправлено в Telegram.");

                    string FileId;
                    if (message.Video != null && message.Video.FileId != null) FileId = message.Video.FileId;
                    else FileId = message.Document.FileId;

                    if (!groupChat) await SendVideoToContacts(FileId, chatId, botClient, caption);
                }
            }
            else
            {
                Console.WriteLine($"Ошибка при скачивании видео: {response.StatusCode}");
                await botClient.SendMessage(chatId, "Ошибка при скачивании видео.");
            }
        }
    }

    private static async Task SendVideoToContacts(string fileId, long telegramId, ITelegramBotClient botClient, string caption = "")
    {
        var contactUserTGIds = await CoreDB.GetContactUserTGIds(await DBforGetters.GetUserIDbyTelegramID(telegramId));
        Console.WriteLine($"Рассылка видео для ({contactUserTGIds.Count}) пользователей.");

        DateTime now = DateTime.Now;
        string name = await DBforGetters.GetUserNameByTelegramID(telegramId);
        string text = $"Ваш контакт {name} отправил видео!\n#{now:yyyy_MM_dd_HH_mm_ss}_{MyRegex().Replace(name, "_")}\n\n{caption}";

        foreach (var contactUserId in contactUserTGIds)
        {
            await botClient.SendDocument(contactUserId, InputFile.FromFileId(fileId), caption: text);
        }

        if (contactUserTGIds.Count > 0) await botClient.SendMessage(telegramId, $"Видео успешно отправлено всем ({contactUserTGIds.Count}) контактам.\n#{now:yyyy_MM_dd_HH_mm_ss}_{MyRegex().Replace(name, "_")}");
    }

    [GeneratedRegex(@"[^a-zA-Zа-яА-Я0-9]")]
    private static partial Regex MyRegex();
}
