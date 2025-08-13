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
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Config.Services;
using TelegramMediaRelayBot.Config;
using Microsoft.Extensions.Options;


namespace TelegramMediaRelayBot;

public partial class TGBot
{
    private readonly IUserRepository _userRepo;
    private readonly IUserGetter _userGetter;
    private readonly CallbackQueryHandlersFactory _handlersFactory;
    private readonly PrivateUpdateHandler _updateHandler;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;
    private readonly ILinkCategorizer _categorizer;
    private readonly IInboxRepository _inboxRepo;
    private readonly IUserFilterService _userFilter;
    private readonly MediaDownloaderService _mediaDownloaderService;
    private readonly TelegramMediaRelayBot.Infrastructure.MediaProcessing.IMediaProcessingService _mediaProcessingService;
    private readonly TelegramMediaRelayBot.TelegramBot.Utils.ITextCleanupService _textCleanupService;
    private readonly TelegramMediaRelayBot.Config.Services.IConfigurationService _configService;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;
    private readonly IOptions<BotConfiguration> _botConfig;
    private readonly IOptionsMonitor<MessageDelayConfiguration> _delayConfig;
    private readonly IOptionsMonitor<TelegramMediaRelayBot.Config.DownloadingConfiguration> _downloadingConfig;
    public static IUserStateManager StateManager { get; private set; }
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, DateTime> _nextSlotByChatId = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, (string Text, DateTime At)> _lastTextByChatId = new();
    public static CancellationToken cancellationToken;

    public TGBot(
        IUserRepository userRepo,
        IUserGetter userGetters,
        CallbackQueryHandlersFactory handlersFactory,
        IContactGetter contactGetterRepository,
        IDefaultActionGetter defaultActionGetter,
        IPrivacySettingsGetter privacySettingsGetter,
        IGroupGetter groupGetter,
        ILinkCategorizer categorizer,
        MediaDownloaderService mediaDownloaderService,
        TelegramMediaRelayBot.Infrastructure.MediaProcessing.IMediaProcessingService mediaProcessingService,
        TelegramMediaRelayBot.Config.Services.IConfigurationService configService,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService,
        IOptions<BotConfiguration> botConfig,
        IOptionsMonitor<MessageDelayConfiguration> delayConfig,
        IOptionsMonitor<TelegramMediaRelayBot.Config.DownloadingConfiguration> downloadingConfig,
        IUserStateManager userStateManager,
        IUserFilterService userFilterService,
        IInboxRepository inboxRepository,
        TelegramMediaRelayBot.TelegramBot.Utils.ITextCleanupService textCleanupService
        )
    {
        _userRepo = userRepo;
        _userGetter = userGetters;
        _handlersFactory = handlersFactory;
        _configService = configService;
        _resourceService = resourceService;
        _privacySettingsGetter = privacySettingsGetter;
        _categorizer = categorizer;
        _userFilter = userFilterService;
        _mediaDownloaderService = mediaDownloaderService;
        _mediaProcessingService = mediaProcessingService;
        _textCleanupService = textCleanupService;
        _inboxRepo = inboxRepository;
        _botConfig = botConfig;
        _delayConfig = delayConfig;
        _downloadingConfig = downloadingConfig;
        
            _updateHandler = new PrivateUpdateHandler(
            this,
            _handlersFactory,
            contactGetterRepository,
            defaultActionGetter,
            _userGetter,
            groupGetter,
            _configService,
            resourceService,
                _textCleanupService,
                _inboxRepo
            );

        StateManager = userStateManager;
    }

    public async Task Start()
    {
        string telegramBotToken = _botConfig.Value.TelegramBotToken;
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

        if (StateManager.TryGet(chatId, out var userState) && userState is not null)
        {
            await userState.ProcessState(botClient, update, cancellationToken);
        }
    }

    public TimeSpan ReserveDelaySlot(long chatId, TimeSpan baseDelay)
    {
        var now = DateTime.UtcNow;
        var next = _nextSlotByChatId.TryGetValue(chatId, out var n) ? n : now;
        var wait = (next > now ? next - now : TimeSpan.Zero) + baseDelay;
        _nextSlotByChatId[chatId] = now + wait;
        return wait;
    }

    public static void RememberLastText(long chatId, string text)
    {
        _lastTextByChatId[chatId] = (text, DateTime.UtcNow);
    }

    public static (bool Found, string Text, DateTime At) TryGetLastText(long chatId)
    {
        if (_lastTextByChatId.TryGetValue(chatId, out var v)) return (true, v.Text, v.At);
        return (false, string.Empty, DateTime.MinValue);
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        LogEvent(update, chatId);

        if (CommonUtilities.CheckPrivateChatType(update))
        {
            if (await TryProcessState(botClient, update)) return;
            await HandlePrivateMessageOrCallback(botClient, update, chatId, cancellationToken);
            return;
        }

        await HandleGroupMessage(botClient, update, cancellationToken);
    }

    private async Task<bool> TryProcessState(ITelegramBotClient botClient, Update update)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (StateManager.Contains(chatId))
        {
            await ProcessState(botClient, update);
            return true;
        }
        return false;
    }

    private async Task HandlePrivateMessageOrCallback(ITelegramBotClient botClient, Update update, long chatId, CancellationToken cancellationToken)
    {
        if (update.Message != null && update.Message.Text != null)
        {
            if (!await EnsureUserHasAccessOrRegister(botClient, update, chatId, cancellationToken))
            {
                return;
            }

            await _updateHandler.ProcessMessage(botClient, update, cancellationToken, chatId);
            return;
        }

        if (update.CallbackQuery != null)
        {
            await _updateHandler.ProcessCallbackQuery(botClient, update, cancellationToken);
        }
    }

    private async Task<bool> EnsureUserHasAccessOrRegister(ITelegramBotClient botClient, Update update, long chatId, CancellationToken cancellationToken)
    {
        bool hasAccess = _userRepo.CheckUserExists(chatId);
        if (hasAccess) return true;

        int usersCount = await _userGetter.GetAllUsersCount();
        string startParameter = CommonUtilities.ParseStartCommand(update.Message!.Text!);
        bool canStart = (usersCount == 0 || !string.IsNullOrEmpty(startParameter)) && _configService.CanUserStartUsingBot(startParameter, _userGetter);

        if (canStart)
        {
            _userRepo.AddUser(update.Message!.Chat.FirstName!, chatId, hasAccess);
            update.Message!.Text = "/start";
            return true;
        }

        if (_botConfig.Value.AccessDeniedMessageContact != " ")
        {
            await botClient.SendMessage(
                chatId,
                string.Format(_resourceService.GetResourceString("AccessDeniedMessage"), _botConfig.Value.AccessDeniedMessageContact),
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Html);
        }

        return false;
    }

    private async Task HandleGroupMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null && update.Message.Text != null && update.Message.Text.Contains('/'))
        {
            GroupUpdateHandler groupUpdateHandler = new GroupUpdateHandler(this, _resourceService);
            await groupUpdateHandler.HandleGroupUpdate(update, botClient, cancellationToken);
        }
    }

    public async Task HandleMediaRequest(ITelegramBotClient botClient, string contentUrl, long chatId, Message statusMessage,
                                                List<long>? targetUserIds = null, bool groupChat = false, string caption = "", CancellationToken? sessionToken = null,
                                                DateTime? originalMessageDateUtc = null)
    {
        var effectiveToken = sessionToken ?? cancellationToken;
        List<byte[]>? mediaFiles = await _mediaDownloaderService.DownloadMedia(botClient, contentUrl, statusMessage, effectiveToken);
        
        if (mediaFiles?.Count > 0)
        {
            Log.Debug($"Downloaded {mediaFiles.Count} files");

            // Применяем политику размера (транскодирование/разбиение) перед отправкой
            Log.Debug("Applying size policy {Policy} (TargetUploadLimitMb={Limit}, TargetPartSizeMb={Part})",
                _downloadingConfig.CurrentValue.IfTooLarge,
                _downloadingConfig.CurrentValue.TargetUploadLimitMb,
                _downloadingConfig.CurrentValue.TargetPartSizeMb);
            var processedFiles = await _mediaProcessingService.ApplySizePolicyAsync(
                mediaFiles,
                _downloadingConfig.CurrentValue,
                effectiveToken);
            Log.Debug("Size policy applied: files before={Before}, after={After}", mediaFiles.Count, processedFiles.Count);

            // Санитизация и лимиты подписи: удаляем HTML, сохраняем Markdown, обрезаем до лимита TG
            var safeCaption = TelegramBot.Utils.CommonUtilities.TrimCaptionToLimit(
                TelegramBot.Utils.CommonUtilities.SanitizeCaptionRemoveHtml(caption));
            await SendMediaToTelegram(botClient, chatId, processedFiles, statusMessage, targetUserIds, contentUrl, groupChat, safeCaption, effectiveToken, originalMessageDateUtc);
            return;
        }

        // Если задача была отменена пользователем — выходим тихо
        if (!(effectiveToken.IsCancellationRequested))
        {
            try
            {
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("FailedToProcessLink"));
            }
            catch (OperationCanceledException) { }
        }
    }

        private async Task SendMediaToTelegram(ITelegramBotClient botClient, long chatId, List<byte[]> mediaFiles,
                                                        Message statusMessage, List<long>? targetUserIds, string contentUrl, bool groupChat = false,
                                                        string caption = "", CancellationToken? sessionToken = null,
                                                        DateTime? originalMessageDateUtc = null)
    {
        var effectiveToken = sessionToken ?? cancellationToken;
        if (!await ValidateMediaFilesOrReport(botClient, statusMessage, mediaFiles)) return;

        var groupedFiles = GroupMediaFiles(mediaFiles);

        try
        {
            await SendGroupedMedia(botClient, chatId, statusMessage, groupedFiles, targetUserIds, contentUrl, groupChat, caption, effectiveToken, originalMessageDateUtc);
            Log.Debug($"Successfully sent {mediaFiles.Count} files");
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException apiEx) when (apiEx.ErrorCode == 413)
        {
            Log.Warning(apiEx, "Telegram returned 413. Trying to resend as separate messages");
            await SendIndividually(botClient, chatId, statusMessage, groupedFiles, contentUrl, caption, effectiveToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to send media group");
            throw;
        }
    }

    private async Task SendIndividually(
        ITelegramBotClient botClient,
        long chatId,
        Message statusMessage,
        Dictionary<TelegramMediaRelayBot.TelegramBot.Utils.MediaFileType, List<byte[]>> groupedFiles,
        string contentUrl,
        string caption,
        CancellationToken? sessionToken = null)
    {
        var effectiveToken = sessionToken ?? cancellationToken;
        foreach (var kv in groupedFiles)
        {
            foreach (var file in kv.Value)
            {
                try
                {
                    var type = CommonUtilities.DetermineFileType(file);
                    var stream = new MemoryStream(file);
                    switch (type)
                    {
                        case TelegramMediaRelayBot.TelegramBot.Utils.MediaFileType.Photo:
                            await botClient.SendPhoto(chatId, new InputFileStream(stream, "photo.jpg"), caption: caption, replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId }, disableNotification: true, cancellationToken: effectiveToken);
                            break;
                        case TelegramMediaRelayBot.TelegramBot.Utils.MediaFileType.Video:
                            await botClient.SendVideo(chatId, new InputFileStream(stream, "video.mp4"), caption: caption, replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId }, disableNotification: true, cancellationToken: effectiveToken);
                            break;
                        case TelegramMediaRelayBot.TelegramBot.Utils.MediaFileType.Audio:
                            await botClient.SendAudio(chatId, new InputFileStream(stream, "audio.mp3"), caption: caption, replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId }, disableNotification: true, cancellationToken: effectiveToken);
                            break;
                        default:
                            await botClient.SendDocument(chatId, new InputFileStream(stream, "file.bin"), caption: caption, replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId }, disableNotification: true, cancellationToken: effectiveToken);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to send individual media item for {Url}", contentUrl);
                }
            }
        }
    }

    private async Task<bool> ValidateMediaFilesOrReport(ITelegramBotClient botClient, Message statusMessage, List<byte[]> mediaFiles)
    {
        // Нет жёсткой отсечки: только информируем общий размер. Ошибку TG перехватываем при отправке
        var totalSize = mediaFiles.Sum(f => f.Length);
        var sizeMB = totalSize / (1024.0 * 1024.0);
        Log.Information("Total file size: {Size:F1}MB (TargetUploadLimitMb={TargetLimit}, Policy={Policy})",
            sizeMB, _downloadingConfig.CurrentValue.TargetUploadLimitMb, _downloadingConfig.CurrentValue.IfTooLarge);
        return true;
    }

    private Dictionary<TelegramMediaRelayBot.TelegramBot.Utils.MediaFileType, List<byte[]>> GroupMediaFiles(List<byte[]> mediaFiles)
    {
        return mediaFiles
            .GroupBy(CommonUtilities.DetermineFileType)
            .ToDictionary(g => g.Key, g => g.Reverse().ToList());
    }

    private async Task SendGroupedMedia(
        ITelegramBotClient botClient,
        long chatId,
        Message statusMessage,
        Dictionary<TelegramMediaRelayBot.TelegramBot.Utils.MediaFileType, List<byte[]>> groupedFiles,
        List<long>? targetUserIds,
        string contentUrl,
        bool groupChat,
        string caption,
        CancellationToken? sessionToken = null,
        DateTime? originalMessageDateUtc = null)
    {
        var effectiveToken = sessionToken ?? cancellationToken;
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
                    cancellationToken: effectiveToken
                );

                List<InputMedia> savedMediaGroupIDs = mess.Select<Message, InputMedia>(msg =>
                                msg.Photo != null ? new InputMediaPhoto(msg.Photo[0].FileId) :
                                msg.Video != null ? new InputMediaVideo(msg.Video.FileId) :
                                msg.Audio != null ? new InputMediaAudio(msg.Audio.FileId) :
                                new InputMediaDocument(msg.Document!.FileId)).ToList();

                var savedMediaRefs = mess.Select(msg =>
                {
                    if (msg.Photo != null) return (Kind: nameof(InputMediaPhoto), FileId: msg.Photo[0].FileId);
                    if (msg.Video != null) return (Kind: nameof(InputMediaVideo), FileId: msg.Video.FileId);
                    if (msg.Audio != null) return (Kind: nameof(InputMediaAudio), FileId: msg.Audio.FileId);
                    return (Kind: nameof(InputMediaDocument), FileId: msg.Document!.FileId);
                }).ToList();

                if (i + 10 >= mediaGroup.Count()) { lastMediaGroup = true; text = caption; }

                if (!groupChat && targetUserIds != null && targetUserIds.Count > 0)
                {
                    await SendVideoToContacts(chatId, botClient, statusMessage, targetUserIds,
                        savedMediaGroupIDs: savedMediaGroupIDs, savedMediaRefs: savedMediaRefs, contentUrl, caption: text,
                        lastMediaGroup: lastMediaGroup, originalMessageDateUtc: originalMessageDateUtc);
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task SendVideoToContacts(
        long telegramId,
        ITelegramBotClient botClient,
        Message statusMessage,
        List<long> targetUserIds,
        List<InputMedia> savedMediaGroupIDs,
        List<(string Kind, string FileId)> savedMediaRefs,
        string contentUrl,
        string caption = "",
        bool lastMediaGroup = false,
        DateTime? originalMessageDateUtc = null
        )
    {
        int userId = _userGetter.GetUserIDbyTelegramID(telegramId);
        bool isDisallowContentForwarding = _privacySettingsGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING);

        List<long> mutedByUserIds = await _userGetter.GetUsersIdForMuteContactIdAsync(userId);
        List<long> filteredContactUserTGIds = targetUserIds.Except(mutedByUserIds).ToList();
        List<long> usersAllowedByLink = await _userFilter.FilterUsersByLink(filteredContactUserTGIds, contentUrl, _categorizer);

        Log.Information($"Sending video to ({usersAllowedByLink.Count}) users.");
        Log.Debug($"User {userId} is muted by: {mutedByUserIds.Count}");

        DateTime now = DateTime.Now;
        string name = _userGetter.GetUserNameByTelegramID(telegramId);
                    string text = string.Format(_resourceService.GetResourceString("ContactSentVideo"), 
                                    name, now.ToString("yyyy_MM_dd_HH_mm_ss"), MyRegex().Replace(name, "_"), caption);
                    string defaultHash1 = now.ToString("yyyy_MM_dd_HH_mm_ss");
                    string defaultHash2 = $"{defaultHash1}_{MyRegex().Replace(name, "_")}";
        int sentCount = 0;

        foreach (long contactUserTgId in usersAllowedByLink)
        {
            try
            {

                int contactUserId = _userGetter.GetUserIDbyTelegramID(contactUserTgId);

                // Inbox gating: if recipient enabled Inbox, store item instead of direct send
                if (_privacySettingsGetter.GetIsActivePrivacyRule(contactUserId, PrivacyRuleType.INBOX_DELIVERY))
                {
                    try
                    {
                        var payload = new
                        {
                            SourceChatId = telegramId,
                            SavedMedia = savedMediaRefs.Select(r => new { Type = r.Kind, FileId = r.FileId, Caption = (string?)null }).ToList(),
                            Url = contentUrl,
                            Caption = caption,
                            Hashtag = new [] { defaultHash1, defaultHash2 },
                            OriginalMessageDateUtc = (originalMessageDateUtc ?? DateTime.UtcNow).ToUniversalTime()
                        };
                        string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
                        await _inboxRepo.AddItemAsync(contactUserId, _userGetter.GetUserIDbyTelegramID(telegramId), caption, payloadJson, "new");
                        Log.Information("Added to Inbox for user {User}", contactUserId);
                        try
                        {
                            int newCount = await _inboxRepo.GetNewCountAsync(contactUserId).ConfigureAwait(false);
                            if (newCount == 1 || newCount % 5 == 0)
                            {
                                string note = string.Format(_resourceService.GetResourceString("InboxNewCountNotify"), newCount);
                                await botClient.SendMessage(contactUserTgId, note, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch { }
                        sentCount++; // count as delivered
                        try
                        {
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
                                $"{filteredContactUserTGIds.Count}/{sentCount}",
                                cancellationToken: cancellationToken);
                        }
                        catch { }
                        continue; // skip direct sending
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to add Inbox item, fallback to direct send");
                    }
                }

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
            await Task.Delay(_delayConfig.CurrentValue.ContactSendDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send video to user {contactUserTgId}: {ex.Message}");
            await Task.Delay(_delayConfig.CurrentValue.ContactSendDelay, cancellationToken);
            }
        }

        if (filteredContactUserTGIds.Count > 0)
        {
            await botClient.SendMessage(telegramId, 
                                            string.Format(_resourceService.GetResourceString("VideoSentToContacts"), 
                                            $"{sentCount}/{filteredContactUserTGIds.Count}", now.ToString("yyyy_MM_dd_HH_mm_ss"),
                                            MyRegex().Replace(name, "_")),
                                            replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId },
                                            parseMode: ParseMode.Html);
        }

        if (mutedByUserIds.Count > 0)
        {
            await botClient.SendMessage(telegramId, 
                                            string.Format(_resourceService.GetResourceString("MutedByContacts"), mutedByUserIds.Count),
                                            replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId });
        }

        await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
                            string.Format(_resourceService.GetResourceString("MessageProcessMediaSend"), sentCount, filteredContactUserTGIds.Count),
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

        if (StateManager.TryGet(chatId, out IUserState? value))
        {
            IUserState userState = value;
            currentUserStatus = userState.GetCurrentState();
        }

        Log.Information($"Event: {logMessageType}, UserId: {userId}, ChatId: {chatId}, {logMessageType}: {logMessageData}, State: {currentUserStatus}");
    }

    [GeneratedRegex(@"[^a-zA-Zа-яА-Я0-9]")]
    private static partial Regex MyRegex();
}