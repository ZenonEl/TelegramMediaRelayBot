using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.SiteFilter;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;
using Telegram.Bot.Exceptions;
using TelegramMediaRelayBot.TelegramBot.Models;
using TelegramMediaRelayBot.Database;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ITelegramSenderService
{
    Task SendMedia(ITelegramBotClient botClient, DownloadSession session, 
                   List<byte[]> mediaFiles, List<long>? targetUserIds, 
                   CancellationToken cancellationToken);
}

public class TelegramSenderService : ITelegramSenderService
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxService _inboxService;
    private readonly IMediaTypeResolver _mediaTypeResolver;
    private readonly IOptionsMonitor<MessageDelayConfiguration> _delayConfig;
    private readonly IResourceService _resourceService;
    private readonly IUserFilterService _userFilter;
    private readonly ILinkCategorizer _categorizer;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;

    public TelegramSenderService(
        IUserGetter userGetter,
        IInboxService inboxService,
        IMediaTypeResolver mediaTypeResolver,
        IOptionsMonitor<MessageDelayConfiguration> delayConfig,
        IResourceService resourceService,
        IUserFilterService userFilter,
        ILinkCategorizer categorizer,
        IPrivacySettingsGetter privacySettingsGetter)
    {
        _userGetter = userGetter;
        _inboxService = inboxService;
        _mediaTypeResolver = mediaTypeResolver;
        _delayConfig = delayConfig;
        _resourceService = resourceService;
        _userFilter = userFilter;
        _categorizer = categorizer;
        _privacySettingsGetter = privacySettingsGetter;
    }

    public async Task SendMedia(ITelegramBotClient botClient, DownloadSession session, List<byte[]> mediaFiles, List<long>? targetUserIds, CancellationToken cancellationToken)
    {
        // 1. ЕДИНОКРАТНАЯ ЗАГРУЗКА: Отправляем байты в чат с ботом, чтобы получить FileId
        var uploadedMedia = await UploadToTelegramStorage(botClient, session.ChatId, mediaFiles, cancellationToken);
        if (!uploadedMedia.Any())
        {
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Failed to prepare media for sending.");
            return;
        }
        
        // Если цели не указаны, отправка только себе уже произошла на шаге 1, просто завершаем.
        if (targetUserIds == null || !targetUserIds.Any())
        {
            // Можно добавить подпись к последнему отправленному сообщению
            return;
        }

        // 2. ФОРМИРУЕМ СПИСОК ПОЛУЧАТЕЛЕЙ
        var senderUserId = _userGetter.GetUserIDbyTelegramID(session.ChatId);
        var mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(senderUserId);
        var filteredTgIds = targetUserIds.Except(mutedByUserIds).ToList();
        var finalRecipients = await _userFilter.FilterUsersByLink(filteredTgIds, session.Url, _categorizer);
        
        // 3. РАССЫЛКА ПО FileId
        int sentCount = 0;
        foreach (var recipientTgId in finalRecipients)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            var recipientUserId = _userGetter.GetUserIDbyTelegramID(recipientTgId);

            // TODO: Логика инбокса должна использовать FileId, а не байты
            // if (await _inboxService.TryDeliverToInbox(...)) { sentCount++; continue; }
            
            // Отправляем медиа по FileId
            await ForwardFromStorage(botClient, recipientTgId, uploadedMedia, session.Caption, senderUserId, cancellationToken);
            sentCount++;
            
            await Task.Delay(_delayConfig.CurrentValue.ContactSendDelay, cancellationToken);
        }
        
        // 4. ФИНАЛЬНЫЙ ОТЧЕТ
        await botClient.SendMessage(session.ChatId, $"Distribution complete. Sent to {sentCount}/{finalRecipients.Count} users.");
    }

    private async Task<List<TelegramMediaInfo>> UploadToTelegramStorage(ITelegramBotClient botClient, long storageChatId, List<byte[]> mediaFiles, CancellationToken cancellationToken)
    {
        var uploadedMedia = new List<TelegramMediaInfo>();
        
        // 1. Строго группируем файлы по типам
        var groupedFiles = mediaFiles
            .Select(bytes => (Bytes: bytes, Type: _mediaTypeResolver.DetermineFileType(bytes)))
            .GroupBy(x => x.Type)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Bytes).ToList());

        // 2. Последовательно отправляем каждую группу
        if (groupedFiles.TryGetValue(TelegramFileType.Photo, out var photos))
        {
            var photoGroup = photos.Select(p => (IAlbumInputMedia)new InputMediaPhoto(new InputFileStream(new MemoryStream(p))));
            var sent = await SendInChunks(botClient, storageChatId, photoGroup.ToList(), null, cancellationToken);
            uploadedMedia.AddRange(sent.Select(m => new TelegramMediaInfo { FileId = m.Photo!.Last().FileId, Type = TelegramFileType.Photo }));
        }
        if (groupedFiles.TryGetValue(TelegramFileType.Video, out var videos))
        {
            var videoGroup = videos.Select(v => (IAlbumInputMedia)new InputMediaVideo(new InputFileStream(new MemoryStream(v))));
            var sent = await SendInChunks(botClient, storageChatId, videoGroup.ToList(), null, cancellationToken);
            uploadedMedia.AddRange(sent.Select(m => new TelegramMediaInfo { FileId = m.Video!.FileId, Type = TelegramFileType.Video }));
        }
        if (groupedFiles.TryGetValue(TelegramFileType.Audio, out var audios))
        {
            foreach(var audioBytes in audios)
            {
                var sent = await botClient.SendAudio(storageChatId, new InputFileStream(new MemoryStream(audioBytes)), cancellationToken: cancellationToken);
                uploadedMedia.Add(new TelegramMediaInfo { FileId = sent.Audio!.FileId, Type = TelegramFileType.Audio });
            }
        }
        // ... аналогично для Document ...

        return uploadedMedia;
    }
    
    private async Task ForwardFromStorage(ITelegramBotClient botClient, long recipientChatId, List<TelegramMediaInfo> uploadedMedia, string caption, int senderUserId, CancellationToken cancellationToken)
    {
        var isDisallowContentForwarding = _privacySettingsGetter.GetIsActivePrivacyRule(senderUserId, PrivacyRuleType.ALLOW_CONTENT_FORWARDING);

        // 1. Строго группируем по типам
        var groupedMedia = uploadedMedia.GroupBy(m => m.Type);

        bool isFirstChunk = true;
        foreach (var group in groupedMedia)
        {
            if (group.Key == TelegramFileType.Photo || group.Key == TelegramFileType.Video)
            {
                var album = group.Select(m => m.Type == TelegramFileType.Photo 
                    ? (IAlbumInputMedia)new InputMediaPhoto(m.FileId)
                    : (IAlbumInputMedia)new InputMediaVideo(m.FileId)).ToList();
                
                // Прикрепляем caption только к первому элементу самой первой группы
                if (isFirstChunk && album.Any())
                {
                    if (album[0] is InputMediaPhoto p) p.Caption = caption;
                    else if (album[0] is InputMediaVideo v) v.Caption = caption;
                    isFirstChunk = false;
                }
                
                await SendInChunks(botClient, recipientChatId, album, caption, cancellationToken, isDisallowContentForwarding);
            }
            else if (group.Key == TelegramFileType.Audio)
            {
                foreach(var audio in group)
                {
                    var audioCaption = isFirstChunk ? caption : null;
                    await botClient.SendAudio(recipientChatId, audio.FileId, caption: audioCaption, cancellationToken: cancellationToken, protectContent: isDisallowContentForwarding);
                    isFirstChunk = false;
                }
            }
            // ... аналогично для Document ...
        }
    }

    private async Task<List<Message>> SendInChunks(ITelegramBotClient botClient, long chatId, List<IAlbumInputMedia> media, string? caption, CancellationToken cancellationToken, bool protectContent = false)
    {
        var allSentMessages = new List<Message>();
        for (int i = 0; i < media.Count; i += 10)
        {
            var chunk = media.Skip(i).Take(10).ToList();
            if (!chunk.Any()) continue;
            
            // Сбрасываем caption для всех, кроме первого элемента первого чанка
            if (i > 0)
            {
                if (chunk[0] is InputMediaPhoto p) p.Caption = null;
                else if (chunk[0] is InputMediaVideo v) v.Caption = null;
            }
            
            var sent = await botClient.SendMediaGroup(chatId, chunk, disableNotification: true, protectContent: protectContent, cancellationToken: cancellationToken);
            allSentMessages.AddRange(sent);
        }
        return allSentMessages;
    }
}