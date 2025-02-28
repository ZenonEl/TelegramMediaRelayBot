// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using TelegramMediaRelayBot;
using System.Text.RegularExpressions;
using DataBase;
using Serilog;
using Telegram.Bot.Types.Enums;


namespace MediaTelegramBot;

public class UserState
{
    public ContactState State { get; set; }
}

partial class TelegramBot
{
    private static ITelegramBotClient? botClient;
    public static Dictionary<long, IUserState> userStates = [];
    public static CancellationToken cancellationToken;
    
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
        cancellationToken = cts.Token;

        botClient.StartReceiving(UpdateHandler, Utils.Utils.ErrorHandler, receiverOptions, cancellationToken: cancellationToken);
        Log.Information("Press any key to exit");
        Console.ReadKey();
        cts.Cancel();
    }

    public static async Task ProcessState(ITelegramBotClient botClient, Update update)
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
                await ProcessState(botClient, update);
                return;
            }

            if (update.Message != null && update.Message.Text != null)
            {
                bool hasAccess = CoreDB.CheckExistsUser(chatId);

                if (!hasAccess)
                {
                    string startParameter = Utils.Utils.ParseStartCommand(update.Message.Text);
                    if (!string.IsNullOrEmpty(startParameter) && Config.CanUserStartUsingBot(startParameter))
                    {
                        CoreDB.AddUser(update.Message.Chat.FirstName!, chatId);
                        update.Message.Text = "/start";
                        hasAccess = true;
                    }
                    else if (Config.showAccessDeniedMessage)
                    {
                        await botClient.SendMessage(chatId, Config.GetResourceString("AccessDeniedMessage"), cancellationToken: cancellationToken);
                    }
                }

                if (hasAccess)
                {
                    await PrivateUpdateHandler.ProcessMessage(botClient, update, cancellationToken, chatId);
                }
            }
            else if (update.CallbackQuery != null)
            {
                await PrivateUpdateHandler.ProcessCallbackQuery(botClient, update, cancellationToken, chatId);
            }
        }
        else 
        {
            if (update.Message != null && update.Message.Text != null && update.Message.Text.Contains('/'))
            {
                await GroupUpdateHandler.HandleGroupUpdate(update, botClient, cancellationToken);
            }
        }
    }

    public static async Task HandleVideoRequest(ITelegramBotClient botClient, string videoUrl, long chatId,
                                                Message statusMessage, List<long>? targetUserIds = null, bool groupChat = false, string caption = "")
    {
        byte[]? videoBytes = await VideoGet.DownloadVideoAsync(botClient, videoUrl, statusMessage, cancellationToken);
        if (videoBytes != null)
        {
            Log.Debug("Video successfully downloaded.");
            await SendVideoToTelegram(videoBytes, chatId, botClient, statusMessage, targetUserIds, groupChat, caption);
            Log.Debug("Video successfully received.");
            return;
        }

        await botClient.SendMessage(chatId, Config.GetResourceString("FailedToProcessLink"));
    }

    public static async Task SendVideoToTelegram(byte[] videoBytes, long chatId, ITelegramBotClient botClient,
                                                Message statusMessage, List<long>? targetUserIds, bool groupChat = false, string caption = "")
    {
        using (var memoryStream = new MemoryStream(videoBytes))
        using (var progressStream = new Utils.ProgressReportingStream(memoryStream))
        {
            long totalBytes = progressStream.Length;
            DateTimeOffset startTime = DateTimeOffset.Now;

            try
            {
                await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
                    Config.GetResourceString("WaitForUpload"),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error editing message.");
            }

            if (Config.showVideoUploadProgress)
            {
                progressStream.OnProgress += (progressMessage) =>
                {
                    Log.Debug($"Video download progress: {progressMessage}");
                };
            }

            progressStream.Position = 0;

            string text = caption != "" ? Config.GetResourceString("WithText") + caption : "";
            if (groupChat)
            {
                User me = await botClient.GetMe();
                DateTime now = DateTime.Now;
                text = string.Format(Config.GetResourceString("ContactSentVideo"), 
                                            me.FirstName, now.ToString("yyyy_MM_dd_HH_mm_ss"), MyRegex().Replace(me.FirstName, "_"), caption);
            }

            Log.Debug("Starting video upload to Telegram.");

            var message = await botClient.SendDocument(
                chatId,
                InputFile.FromStream(progressStream, "video.mp4"),
                caption: Config.GetResourceString("HereIsYourVideo") + text,
                replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId },
                parseMode: ParseMode.Html
            );

            Log.Debug("Video successfully sent to Telegram.");

            string FileId;
            if (message.Video != null && message.Video.FileId != null)
                FileId = message.Video.FileId;
            else
                FileId = message.Document!.FileId;

            if (!groupChat && targetUserIds != null && targetUserIds.Count > 0)
            {
                try
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
                        Config.GetResourceString("WaitSendingVideo"),
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Error editing message.");
                }
                Log.Debug("Starting video distribution to contacts.");
                await SendVideoToContacts(FileId, chatId, botClient, statusMessage, targetUserIds, caption);
                try
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
                        Config.GetResourceString("VideoSuccessfullySent"),
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Error editing message.");
                }
            }
        }
    }

    private static async Task SendVideoToContacts(string fileId, long telegramId, ITelegramBotClient botClient, Message statusMessage, List<long> targetUserIds, string caption = "")
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(telegramId);
        List<long> mutedByUserIds = DBforGetters.GetUsersIdForMuteContactId(userId);
        List<long> filteredContactUserTGIds = targetUserIds.Except(mutedByUserIds).ToList();

        Log.Information($"Sending video to ({filteredContactUserTGIds.Count}) users.");
        Log.Information($"User {userId} is muted by: {mutedByUserIds.Count}");

        DateTime now = DateTime.Now;
        string name = DBforGetters.GetUserNameByTelegramID(telegramId);
        string text = string.Format(Config.GetResourceString("ContactSentVideo"), 
                                    name, now.ToString("yyyy_MM_dd_HH_mm_ss"), MyRegex().Replace(name, "_"), caption);
        int sentCount = 0;

        foreach (var contactUserId in filteredContactUserTGIds)
        {
            try
            {
                await botClient.SendDocument(contactUserId, InputFile.FromFileId(fileId), caption: text, parseMode: ParseMode.Html);
                sentCount++;

                try
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
                        $"{filteredContactUserTGIds.Count}/{sentCount}",
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Error editing message.");
                }
                Log.Information($"Sent video to user {contactUserId}. Total sent: {sentCount}/{filteredContactUserTGIds.Count}");
                await Task.Delay(Config.contactSendDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send video to user {contactUserId}: {ex.Message}");
                await Task.Delay(Config.contactSendDelay, cancellationToken);
            }
        }

        if (filteredContactUserTGIds.Count > 0)
        {
            await botClient.SendMessage(telegramId, 
                                            string.Format(Config.GetResourceString("VideoSentToContacts"), 
                                            $"{sentCount}/{filteredContactUserTGIds.Count}", now.ToString("yyyy_MM_dd_HH_mm_ss"),
                                            MyRegex().Replace(name, "_")),
                                            replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId },
                                            parseMode: ParseMode.Html);
        }

        if (mutedByUserIds.Count > 0)
        {
            await botClient.SendMessage(telegramId, 
                                            string.Format(Config.GetResourceString("MutedByContacts"), mutedByUserIds.Count),
                                            replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId });
        }
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