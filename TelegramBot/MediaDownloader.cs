// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using System.Text.RegularExpressions;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot.SiteFilter;
using TelegramMediaRelayBot.TelegramBot.Downloaders;


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
    private readonly MediaDownloadService _downloadService;
    private readonly IDownloadJobRepository _jobRepository;
    // User states are now managed by UserSessionManager
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
        MediaDownloadService downloadService,
        IDownloadJobRepository jobRepository
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
        _downloadService = downloadService;
        _jobRepository = jobRepository;
    }

    public static async Task ProcessState(ITelegramBotClient botClient, Update update)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (UserSessionManager.Get(chatId) is IUserState userState)
        {
            await userState.ProcessState(botClient, update, cancellationToken);
        }
    }

    public async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        LogEvent(update, chatId);

        if (CommonUtilities.CheckPrivateChatType(update))
        {
            // Media session callbacks are handled by the factory, not by user state
            if (update.CallbackQuery != null && IsMediaSessionCallback(update.CallbackQuery.Data))
            {
                await _updateHandler.ProcessCallbackQuery(botClient, update, cancellationToken);
                return;
            }

            if (UserSessionManager.ContainsKey(chatId))
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
                        await botClient.SendMessage(chatId, string.Format(Localization.Get("AccessDeniedMessage"), Config.accessDeniedMessageContact), cancellationToken: cancellationToken, parseMode: ParseMode.Html);
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
            if (update.Message != null && update.Message.Text != null)
            {
                GroupUpdateHandler groupUpdateHandler = new GroupUpdateHandler(this);
                await groupUpdateHandler.HandleGroupUpdate(update, botClient, cancellationToken);
            }
        }
    }

    public async Task HandleMediaRequest(ITelegramBotClient botClient, string contentUrl, long chatId, Message statusMessage,
                                                List<long>? targetUserIds = null, bool groupChat = false, string caption = "",
                                                string? persistedJobId = null)
    {
        // Persist the job so a crash or reboot mid-download does not lose the request;
        // DownloadJobResumeService re-enqueues leftovers at startup.
        string jobId = persistedJobId ?? Guid.NewGuid().ToString("N");
        if (persistedJobId is null)
            TryPersistJob(new DownloadJob(jobId, chatId, contentUrl, caption, targetUserIds, groupChat));

        var progress = BuildStatusProgress(botClient, statusMessage, cancellationToken);

        using (MediaDownloadResult? result = await DownloadQueue.EnqueueAsync(
            () => _downloadService.DownloadAsync(contentUrl, progress, cancellationToken),
            position => EditStatus(botClient, statusMessage, $"⏳ Queued (#{position})", cancellationToken),
            cancellationToken))
        {
            if (result is { Files.Count: > 0 })
            {
                Log.Debug($"Downloaded {result.Files.Count} files");
                await SendMediaToTelegram(botClient, chatId, result.Files, statusMessage, targetUserIds, contentUrl, groupChat, caption);
            }
            else
            {
                await botClient.SendMessage(chatId, Localization.Get("FailedToProcessLink"));
            }
        }

        // Reached only on normal completion (sent or reported as failed);
        // on crash/shutdown the row stays and the job is resumed after restart.
        TryRemoveJob(jobId);
    }

    private void TryPersistJob(DownloadJob job)
    {
        try { _jobRepository.Add(job); }
        catch (Exception ex) { Log.Error(ex, "Failed to persist download job {JobId}", job.Id); }
    }

    private void TryRemoveJob(string jobId)
    {
        try { _jobRepository.Remove(jobId); }
        catch (Exception ex) { Log.Error(ex, "Failed to remove download job {JobId}", jobId); }
    }

    private async Task SendMediaToTelegram(ITelegramBotClient botClient, long chatId, IReadOnlyList<MediaFile> mediaFiles,
                                                        Message statusMessage, List<long>? targetUserIds, string contentUrl, bool groupChat = false,
                                                        string caption = "")
    {
        var groups = mediaFiles.GroupBy(f => f.Kind).ToList();

        try
        {
            string text = string.Empty;
            bool lastMediaGroup = false;

            foreach (var group in groups)
            {
                var files = group.ToList();

                for (int i = 0; i < files.Count; i += 10)
                {
                    var chunk = files.Skip(i).Take(10).ToList();
                    var streams = chunk.Select(f => (Stream)File.OpenRead(f.Path)).ToList();
                    try
                    {
                        var album = chunk.Zip(streams, (f, s) =>
                            CreateAlbumItem(f.Kind, s, Path.GetFileName(f.Path))).ToList();

                        Message[] mess = await botClient.SendMediaGroup(
                            chatId: chatId,
                            media: album,
                            replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId },
                            disableNotification: true,
                            cancellationToken: cancellationToken
                        );

                        List<InputMedia> savedMediaGroupIDs = mess.Select<Message, InputMedia>(msg =>
                                        msg.Photo != null ? new InputMediaPhoto(msg.Photo[0].FileId) :
                                        msg.Video != null ? new InputMediaVideo(msg.Video.FileId) :
                                        msg.Audio != null ? new InputMediaAudio(msg.Audio.FileId) :
                                        new InputMediaDocument(msg.Document!.FileId)).ToList();

                        if (i + 10 >= files.Count) { lastMediaGroup = true; text = caption; }

                        if (!groupChat && targetUserIds != null && targetUserIds.Count > 0)
                            await SendVideoToContacts(chatId, botClient, statusMessage, targetUserIds,
                                savedMediaGroupIDs: savedMediaGroupIDs, contentUrl, caption: text,
                                lastMediaGroup: lastMediaGroup);
                    }
                    finally
                    {
                        foreach (var s in streams) await s.DisposeAsync();
                    }

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

    private static IAlbumInputMedia CreateAlbumItem(MediaKind kind, Stream stream, string fileName) => kind switch
    {
        MediaKind.Photo => new InputMediaPhoto(InputFile.FromStream(stream, fileName)),
        MediaKind.Video => new InputMediaVideo(InputFile.FromStream(stream, fileName)),
        MediaKind.Audio => new InputMediaAudio(InputFile.FromStream(stream, fileName)),
        _ => new InputMediaDocument(InputFile.FromStream(stream, fileName)),
    };

    private static IProgress<string> BuildStatusProgress(ITelegramBotClient botClient, Message statusMessage, CancellationToken ct)
    {
        DateTime last = DateTime.MinValue;
        string lastText = "";
        return new Progress<string>(text =>
        {
            if (DateTime.UtcNow - last < TimeSpan.FromMilliseconds(Config.videoGetDelay)) return;
            if (text == lastText) return;
            last = DateTime.UtcNow;
            lastText = text;
            if (Config.showVideoDownloadProgress) Log.Debug("Download progress: {Text}", text);
            EditStatus(botClient, statusMessage, text, ct);
        });
    }

    private static void EditStatus(ITelegramBotClient botClient, Message statusMessage, string text, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, text, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error editing status message.");
            }
        }, ct);
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
        string text = string.Format(Localization.Get("ContactSentVideo"), 
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
                                            string.Format(Localization.Get("VideoSentToContacts"), 
                                            $"{sentCount}/{filteredContactUserTGIds.Count}", now.ToString("yyyy_MM_dd_HH_mm_ss"),
                                            MyRegex().Replace(name, "_")),
                                            replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId },
                                            parseMode: ParseMode.Html);
        }

        if (mutedByUserIds.Count > 0)
        {
            await botClient.SendMessage(telegramId, 
                                            string.Format(Localization.Get("MutedByContacts"), mutedByUserIds.Count),
                                            replyParameters: new ReplyParameters { MessageId = statusMessage.MessageId });
        }

        await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId,
            string.Format(Localization.Get("MessageProcessMediaSend"), sentCount, filteredContactUserTGIds.Count),
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
                    if (!update.Message.Text.Contains("/link") && !update.Message.Text.Contains("/help")
                        && !CommonUtilities.IsLink(update.Message.Text.Split('\n')[0].Trim())) return;
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

        if (UserSessionManager.TryGetValue(chatId, out IUserState? value))
        {
            IUserState userState = value;
            currentUserStatus = userState.GetCurrentState();
        }

        Log.Information($"Event: {logMessageType}, UserId: {userId}, ChatId: {chatId}, {logMessageType}: {logMessageData}, State: {currentUserStatus}");
    }

    private static readonly string[] _mediaSessionPrefixes = new[]
    {
        "send_to_all_contacts:",
        "send_to_default_groups:",
        "send_to_specified_groups:",
        "send_to_specified_users:",
        "send_only_to_me:",
        "cancel_media:",
    };

    private static bool IsMediaSessionCallback(string? data)
    {
        if (string.IsNullOrEmpty(data)) return false;
        foreach (var prefix in _mediaSessionPrefixes)
        {
            if (data.StartsWith(prefix)) return true;
        }
        return false;
    }

    [GeneratedRegex(@"[^a-zA-Zа-яА-Я0-9]")]
    private static partial Regex MyRegex();
}