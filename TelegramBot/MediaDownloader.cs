// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using DataBase.Types;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot;

public partial class TGBot
{
    private readonly IUserRepository _userRepo;
    private readonly IUserGettersRepository _userGettersRepo;
    public static Dictionary<long, IUserState> userStates = [];
    public static CancellationToken cancellationToken;

    public TGBot(
        IUserRepository userRepo,
        IUserGettersRepository userGettersRepo)
    {
        _userRepo = userRepo;
        _userGettersRepo = userGettersRepo;
    }

    public async Task Start()
    {
        string telegramBotToken = Config.telegramBotToken!;
        ITelegramBotClient _botClient = new TelegramBotClient(telegramBotToken);

        var me = await _botClient.GetMe();
        Log.Information($"Hello, I am {me.Id} ready and my name is {me.FirstName}.");

        var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }
        };
        cancellationToken = cts.Token;

        _botClient.StartReceiving(
            updateHandler: UpdateHandler,
            errorHandler: CommonUtilities.ErrorHandler,
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token
        );
    }

    public static async Task ProcessState(ITelegramBotClient botClient, Update update)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (userStates[chatId] is IUserState userState)
        {
            await userState.ProcessState(botClient, update, cancellationToken);
        }
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        LogEvent(update, chatId);

        if (CommonUtilities.CheckPrivateChatType(update))
        {
            if (userStates.ContainsKey(chatId))
            {
                await ProcessState(botClient, update);
                return;
            }

            if (update.Message != null && update.Message.Text != null)
            {
                bool hasAccess = _userRepo.CheckUserExists(chatId);

                if (!hasAccess)
                {
                    string startParameter = CommonUtilities.ParseStartCommand(update.Message.Text);
                    if (!string.IsNullOrEmpty(startParameter) && Config.CanUserStartUsingBot(startParameter))
                    {
                        _userRepo.AddUser(update.Message.Chat.FirstName!, chatId, hasAccess);
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
                await PrivateUpdateHandler.ProcessCallbackQuery(botClient, update, cancellationToken);
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

    public async Task HandleMediaRequest(ITelegramBotClient botClient, string videoUrl, long chatId, Message statusMessage,
                                                List<long>? targetUserIds = null, bool groupChat = false, string caption = "")
    {
        List<byte[]>? mediaFiles = await VideoGet.DownloadMedia(botClient, videoUrl, statusMessage, cancellationToken);
        if (mediaFiles?.Count > 0)
        {
            Log.Debug($"Downloaded {mediaFiles.Count} files");
            await SendMediaToTelegram(botClient, chatId, mediaFiles, statusMessage, targetUserIds, groupChat, caption);
            return;
        }

        await botClient.SendMessage(chatId, Config.GetResourceString("FailedToProcessLink"));
    }

    private async Task SendMediaToTelegram(ITelegramBotClient botClient, long chatId, List<byte[]> mediaFiles,
                                                        Message statusMessage, List<long>? targetUserIds, bool groupChat = false,
                                                        string caption = "")
    {
        var groupedFiles = mediaFiles.GroupBy(CommonUtilities.DetermineFileType)
                                    .ToDictionary(g => g.Key, g => g.Reverse().ToList());

        try
        {
            string text = string.Empty;
            bool lastMediaGroup = false;

            foreach (var fileGroup in groupedFiles.Values)
            {
                var mediaGroup = CommonUtilities.CreateMediaGroup(fileGroup);
                
                for (int i = 0; i < mediaGroup.Count(); i += 10)
                {
                    var chunk = mediaGroup.Skip(i).Take(10).ToList();
                    
                    Message[] mess = await botClient.SendMediaGroup(
                        chatId: chatId,
                        media: chunk,
                        replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId },
                        disableNotification: true,
                        cancellationToken: cancellationToken
                    );

                    List<InputMedia> savedMediaGroupIDs = mess.Select<Message, InputMedia>(msg => 
                                    msg.Photo != null ? new InputMediaPhoto(msg.Photo[0].FileId) :
                                    msg.Video != null ? new InputMediaVideo(msg.Video.FileId) :
                                    msg.Audio != null ? new InputMediaAudio(msg.Audio.FileId) :
                                    new InputMediaDocument(msg.Document!.FileId)).ToList();

                    if (i + 10 >= mediaGroup.Count()) { lastMediaGroup = true; text = caption; }

                    if (!groupChat && targetUserIds != null && targetUserIds.Count > 0) 
                        await SendVideoToContacts(chatId, botClient, statusMessage, targetUserIds,
                            savedMediaGroupIDs: savedMediaGroupIDs, caption: text,
                            lastMediaGroup: lastMediaGroup);
                    
                    await Task.Delay(1000, cancellationToken);
                }
            }

            Log.Debug($"Successfully sent {mediaFiles.Count} files");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to send media group");
            throw;
        }
    }

    private async Task SendVideoToContacts(long telegramId, ITelegramBotClient botClient,
                                                Message statusMessage, List<long> targetUserIds,
                                                List<InputMedia> savedMediaGroupIDs, string caption = "", 
                                                bool lastMediaGroup = false)
    {
        int userId = _userGettersRepo.GetUserIDbyTelegramID(telegramId);
        List<long> mutedByUserIds = _userGettersRepo.GetUsersIdForMuteContactId(userId);
        List<long> filteredContactUserTGIds = targetUserIds.Except(mutedByUserIds).ToList();
        bool isDisallowContentForwarding = PrivacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.AllowContentForwarding);

        Log.Information($"Sending video to ({filteredContactUserTGIds.Count}) users.");
        Log.Information($"User {userId} is muted by: {mutedByUserIds.Count}");

        DateTime now = DateTime.Now;
        string name = _userGettersRepo.GetUserNameByTelegramID(telegramId);
        string text = string.Format(Config.GetResourceString("ContactSentVideo"), 
                                    name, now.ToString("yyyy_MM_dd_HH_mm_ss"), MyRegex().Replace(name, "_"), caption);
        int sentCount = 0;

        foreach (long contactUserTgId in filteredContactUserTGIds)
        {
            try
            {

                int contactUserId = _userGettersRepo.GetUserIDbyTelegramID(contactUserTgId);

                Message[] mediaMessages = await botClient.SendMediaGroup(contactUserTgId,
                                                                        savedMediaGroupIDs.Select(media => (IAlbumInputMedia)media),
                                                                        disableNotification: true,
                                                                        protectContent: isDisallowContentForwarding);
                if (lastMediaGroup)
                {
                    await botClient.SendMessage(
                        chatId: contactUserTgId,
                        text: text,
                        parseMode: ParseMode.Html,
                        replyParameters: new ReplyParameters { MessageId = mediaMessages[0].MessageId },
                        protectContent: isDisallowContentForwarding
                    );
                }
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
                Log.Information($"Sent video to user {contactUserTgId}. Total sent: {sentCount}/{filteredContactUserTGIds.Count}");
                await Task.Delay(Config.contactSendDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send video to user {contactUserTgId}: {ex.Message}");
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
            if (!CommonUtilities.CheckPrivateChatType(update))
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