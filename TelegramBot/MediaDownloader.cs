using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using TikTokMediaRelayBot;
using System.Text.RegularExpressions;
using DataBase;
using Serilog;
using System.Globalization;

namespace MediaTelegramBot;

public class UserState
{
    public ContactState State { get; set; }
}

partial class TelegramBot
{
    private static ITelegramBotClient? botClient;
    public static Dictionary<long, IUserState> userStates = new Dictionary<long, IUserState>();
    
    static public async Task Start()
    {
        string telegramBotToken = Config.telegramBotToken!;
        botClient = new TelegramBotClient(telegramBotToken);

        var me = await botClient.GetMe();
        Log.Information($"Hello, I am {me.Id} ready and my name is {me.FirstName}.");

        var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }
        };

        botClient.StartReceiving(UpdateHandler, Utils.Utils.ErrorHandler, receiverOptions, cancellationToken: cts.Token);
        Log.Information("Press any key to exit");
        Console.ReadKey();
        cts.Cancel();
    }

    public static async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

        if (userStates[chatId] is IUserState userState)
        {
            await userState.ProcessState(botClient, update, cancellationToken);
        }
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

        LogEvent(update, chatId);

        if (Utils.Utils.CheckPrivateChatType(update))
        {
            if (userStates.ContainsKey(chatId))
            {
                await ProcessState(botClient, update, cancellationToken);
                return;
            }

            if (update.Message != null && update.Message.Text != null)
            {
                CoreDB.AddUser(update.Message.Chat.FirstName!, update.Message.Chat.Id);
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
        byte[]? videoBytes = await VideoGet.DownloadVideoAsync(videoUrl);
        if (videoBytes != null)
        {
            await SendVideoToTelegram(videoBytes, chatId, botClient, groupChat, caption);
            Log.Debug("Video successfully received.");
            return;
        }

        await botClient.SendMessage(chatId, Config.resourceManager.GetString("FailedToProcessLink", CultureInfo.CurrentUICulture)!);
    }

    public static async Task SendVideoToTelegram(byte[] videoBytes, long chatId, ITelegramBotClient botClient, bool groupChat = false, string caption = "")
    {
        using (var stream = new MemoryStream(videoBytes))
        {
            stream.Position = 0;
            string text = caption != "" ? Config.resourceManager.GetString("WithText", CultureInfo.CurrentUICulture)! + caption : "";

            var message = await botClient.SendDocument(chatId, InputFile.FromStream(stream, "video.mp4"), 
                                                        caption: Config.resourceManager.GetString("HereIsYourVideo", 
                                                        CultureInfo.CurrentUICulture)! + text);
            Log.Debug("Video successfully sent to Telegram.");

            string FileId;
            if (message.Video != null && message.Video.FileId != null)
                FileId = message.Video.FileId;
            else
                FileId = message.Document!.FileId;

            if (!groupChat) await SendVideoToContacts(FileId, chatId, botClient, caption);
        }
    }

    private static async Task SendVideoToContacts(string fileId, long telegramId, ITelegramBotClient botClient, string caption = "")
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(telegramId);
        var contactUserTGIds = await CoreDB.GetContactUserTGIds(userId);
        var mutedByUserIds = DBforGetters.GetUsersIdForMuteContactId(userId);
        var filteredContactUserTGIds = contactUserTGIds.Except(mutedByUserIds).ToList();

        Log.Information($"Sending video to ({filteredContactUserTGIds.Count}) users.");
        Log.Information($"User {userId} is muted by: {mutedByUserIds.Count}");

        DateTime now = DateTime.Now;
        string name = DBforGetters.GetUserNameByTelegramID(telegramId);
        string text = string.Format(Config.resourceManager.GetString("ContactSentVideo", CultureInfo.CurrentUICulture)!, 
                                    name, now.ToString("yyyy_MM_dd_HH_mm_ss"), MyRegex().Replace(name, "_"), caption);

        foreach (var contactUserId in filteredContactUserTGIds)
        {
            await botClient.SendDocument(contactUserId, InputFile.FromFileId(fileId), caption: text);
        }

        if (filteredContactUserTGIds.Count > 0) await botClient.SendMessage(telegramId, 
                                                                            string.Format(Config.resourceManager.GetString("VideoSentToContacts", 
                                                                            CultureInfo.CurrentUICulture)!, 
                                                                            filteredContactUserTGIds.Count, now.ToString("yyyy_MM_dd_HH_mm_ss"), 
                                                                            MyRegex().Replace(name, "_")));
        if (mutedByUserIds.Count > 0) await botClient.SendMessage(telegramId, 
                                                                string.Format(Config.resourceManager.GetString("MutedByContacts", 
                                                                CultureInfo.CurrentUICulture)!, mutedByUserIds.Count));

    }

    public static void LogEvent(Update update, long chatId)
    {
        string currentUserStatus = "";
        string logMessage = "";
        string callbackData = "";
        long userId = 0;

        if (update.CallbackQuery != null)
        {
            logMessage = "CallbackQuery";
            callbackData = update.CallbackQuery.Data!;
            userId = update.CallbackQuery.From.Id;
        }
        else if (update.Message != null && update.Message.Text != null)
        {
            logMessage = "Message";
            callbackData = update.Message.Text;
            userId = update.Message.From!.Id;
            if (!Utils.Utils.CheckPrivateChatType(update))
            {
                if (!update.Message.Text.Contains("/link") || !update.Message.Text.Contains("/help")) return;
            }
        }

        if (userStates.TryGetValue(chatId, out IUserState? value))
        {
            IUserState userState = value;
            currentUserStatus = userState.GetCurrentState();
        }

        Log.Information($"Event: {logMessage}, UserId: {userId}, ChatId: {chatId}, {logMessage}: {callbackData}, State: {currentUserStatus}");
    }

    [GeneratedRegex(@"[^a-zA-Zа-яА-Я0-9]")]
    private static partial Regex MyRegex();
}