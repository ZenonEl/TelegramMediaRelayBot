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
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot.SiteFilter;


namespace TelegramMediaRelayBot;

public partial class TGBot
{
    private readonly IUserRepository _userRepo;
    private readonly IUserGetter _userGetter;
    private readonly CallbackQueryHandlersFactory _handlersFactory;
    private readonly PrivateUpdateHandler _updateHandler;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;
    private readonly ILinkCategorizer _categorizer;
    private readonly IUserFilterService _userFilter;
    public static Dictionary<long, IUserState> userStates = [];
    public static CancellationToken cancellationToken;

    public TGBot(
        IUserRepository userRepo,
        IUserGetter userGetters,
        CallbackQueryHandlersFactory handlersFactory,
        IContactGetter contactGetterRepository,
        IDefaultActionGetter defaultActionGetter,
        IPrivacySettingsGetter privacySettingsGetter,
        IGroupGetter groupGetter,
        ILinkCategorizer categorizer
        )
    {
        _userRepo = userRepo;
        _userGetter = userGetters;
        _handlersFactory = handlersFactory;
        _updateHandler = new PrivateUpdateHandler(
            this,
            _handlersFactory,
            contactGetterRepository,
            defaultActionGetter,
            _userGetter,
            groupGetter
            );
        _privacySettingsGetter = privacySettingsGetter;
        _categorizer = categorizer;
        _userFilter = new DefaultUserFilterService(_userGetter, _privacySettingsGetter);
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
                    int usersCount = await _userGetter.GetAllUsersCount();
                    string startParameter = CommonUtilities.ParseStartCommand(update.Message.Text);
                    if ((usersCount == 0 || !string.IsNullOrEmpty(startParameter)) && Config.CanUserStartUsingBot(startParameter, _userGetter))
                    {
                        _userRepo.AddUser(update.Message.Chat.FirstName!, chatId, hasAccess);
                        update.Message.Text = "/start";
                        hasAccess = true;
                    }
                    else if (Config.showAccessDeniedMessage)
                    {
                        await botClient.SendMessage(chatId, string.Format(Config.GetResourceString("AccessDeniedMessage"), Config.accessDeniedMessageContact), cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                    }
                }

                if (hasAccess)
                {
                    await _updateHandler.ProcessMessage(botClient, update, cancellationToken, chatId);
                }
            }
            else if (update.CallbackQuery != null)
            {
                await _updateHandler.ProcessCallbackQuery(botClient, update, cancellationToken);
            }
        }
        else 
        {
            if (update.Message != null && update.Message.Text != null && update.Message.Text.Contains('/'))
            {
                GroupUpdateHandler groupUpdateHandler = new GroupUpdateHandler(this);
                await groupUpdateHandler.HandleGroupUpdate(update, botClient, cancellationToken);
            }
        }
    }

    public async Task HandleMediaRequest(ITelegramBotClient botClient, string contentUrl, long chatId, Message statusMessage,
                                                List<long>? targetUserIds = null, bool groupChat = false, string caption = "")
    {
        List<byte[]>? mediaFiles = await MediaGet.DownloadMedia(botClient, contentUrl, statusMessage, cancellationToken);
        if (mediaFiles?.Count > 0)
        {
            Log.Debug($"Downloaded {mediaFiles.Count} files");
            await SendMediaToTelegram(botClient, chatId, mediaFiles, statusMessage, targetUserIds, contentUrl, groupChat, caption);
            return;
        }

        await botClient.SendMessage(chatId, Config.GetResourceString("FailedToProcessLink"));
    }

    private async Task SendMediaToTelegram(ITelegramBotClient botClient, long chatId, List<byte[]> mediaFiles,
                                                        Message statusMessage, List<long>? targetUserIds, string contentUrl, bool groupChat = false,
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
                            savedMediaGroupIDs: savedMediaGroupIDs, contentUrl, caption: text,
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

    private async Task SendVideoToContacts(
        long telegramId,
        ITelegramBotClient botClient,
        Message statusMessage,
        List<long> targetUserIds,
        List<InputMedia> savedMediaGroupIDs,
        string contentUrl,
        string caption = "",
        bool lastMediaGroup = false
        )
    {
        int userId = _userGetter.GetUserIDbyTelegramID(telegramId);
        bool isDisallowContentForwarding = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING);

        List<long> mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);
        List<long> filteredContactUserTGIds = targetUserIds.Except(mutedByUserIds).ToList();
        List<long> usersAllowedByLink = await _userFilter.FilterUsersByLink(filteredContactUserTGIds, contentUrl, _categorizer);

        Log.Information($"Sending video to ({usersAllowedByLink.Count}) users.");
        Log.Debug($"User {userId} is muted by: {mutedByUserIds.Count}");

        DateTime now = DateTime.Now;
        string name = _userGetter.GetUserNameByTelegramID(telegramId);
        string text = string.Format(Config.GetResourceString("ContactSentVideo"), 
                                    name, now.ToString("yyyy_MM_dd_HH_mm_ss"), MyRegex().Replace(name, "_"), caption);
        int sentCount = 0;

        foreach (long contactUserTgId in usersAllowedByLink)
        {
            try
            {

                int contactUserId = _userGetter.GetUserIDbyTelegramID(contactUserTgId);

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

        await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
            string.Format(Config.GetResourceString("MessageProcessMediaSend"), sentCount, filteredContactUserTGIds.Count),
            cancellationToken: cancellationToken);
    }

    public static void LogEvent(Update update, long chatId)
    {
        string currentUserStatus = "";
        string logMessageType;
        string logMessageData;
        long userId;

        if (update.CallbackQuery != null)
        {
            logMessageType = "CallbackQuery";
            logMessageData = update.CallbackQuery.Data!;
            userId = update.CallbackQuery.From.Id;
        }
        else if (update.Message != null)
        {
            if (update.Message.Text != null)
            {
                logMessageType = "Message";
                logMessageData = update.Message.Text;
                userId = update.Message.From!.Id;
                
                if (!CommonUtilities.CheckPrivateChatType(update))
                {
                    if (!update.Message.Text.Contains("/link") && !update.Message.Text.Contains("/help")) return;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (userStates.TryGetValue(chatId, out IUserState? value))
        {
            IUserState userState = value;
            currentUserStatus = userState.GetCurrentState();
        }

        Log.Information($"Event: {logMessageType}, UserId: {userId}, ChatId: {chatId}, {logMessageType}: {logMessageData}, State: {currentUserStatus}");
    }

    [GeneratedRegex(@"[^a-zA-Zа-яА-Я0-9]")]
    private static partial Regex MyRegex();
}