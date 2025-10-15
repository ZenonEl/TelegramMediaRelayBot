using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.SiteFilter;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;
using Telegram.Bot.Exceptions;

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
    private readonly IContactGetter _contactGetter;
    private readonly IUserFilterService _userFilter;
    private readonly ILinkCategorizer _categorizer;
    private readonly IInboxService _inboxService;
    private readonly IMediaTypeResolver _mediaTypeResolver;
    private readonly IOptionsMonitor<MessageDelayConfiguration> _delayConfig;
    private readonly IResourceService _resourceService;


    public TelegramSenderService(
        IUserGetter userGetter,
        IContactGetter contactGetter,
        IUserFilterService userFilter,
        ILinkCategorizer categorizer,
        IInboxService inboxService,
        IMediaTypeResolver mediaTypeResolver,
        IOptionsMonitor<MessageDelayConfiguration> delayConfig,
        IResourceService resourceService)
    {
        _userGetter = userGetter;
        _contactGetter = contactGetter;
        _userFilter = userFilter;
        _categorizer = categorizer;
        _inboxService = inboxService;
        _mediaTypeResolver = mediaTypeResolver;
        _delayConfig = delayConfig;
        _resourceService = resourceService;
    }

    public async Task SendMedia(ITelegramBotClient botClient, DownloadSession session, List<byte[]> mediaFiles, List<long>? targetUserIds, CancellationToken cancellationToken)
    {
        var uploadedMedia = await UploadToTelegramStorage(botClient, session.ChatId, mediaFiles, cancellationToken);
        if (!uploadedMedia.Any())
        {
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Failed to prepare media for sending.");
            return;
        }
        
        if (targetUserIds == null)
        {
            //await ForwardFromStorage(botClient, session.ChatId, uploadedMedia, session.Caption, cancellationToken);
            return;
        }

        var senderUserId = _userGetter.GetUserIDbyTelegramID(session.ChatId);
        var mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(senderUserId);
        var filteredTgIds = targetUserIds.Except(mutedByUserIds).ToList();
        var finalRecipients = await _userFilter.FilterUsersByLink(filteredTgIds, session.Url, _categorizer);

        int sentCount = 0;
        foreach (var recipientTgId in finalRecipients)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            var recipientUserId = _userGetter.GetUserIDbyTelegramID(recipientTgId);

            if (await _inboxService.TryDeliverToInbox(botClient, session, recipientUserId, uploadedMedia))
            {
                sentCount++;
                continue;
            }
            
            await ForwardFromStorage(botClient, recipientTgId, uploadedMedia, session.Caption, cancellationToken);
            sentCount++;
            await Task.Delay(_delayConfig.CurrentValue.ContactSendDelay, cancellationToken);
        }

        await botClient.SendMessage(session.ChatId, $"Distribution complete. Sent to {sentCount}/{finalRecipients.Count} users.");
    }

    private async Task<List<(string Kind, string FileId)>> UploadToTelegramStorage(ITelegramBotClient botClient, long storageChatId, List<byte[]> mediaFiles, CancellationToken cancellationToken)
    {
        var mediaGroup = _mediaTypeResolver.CreateMediaGroup(mediaFiles).ToList();
        if (!mediaGroup.Any()) return new();
        
        var sentMessages = await SendInChunks(botClient, storageChatId, mediaGroup, null, cancellationToken);
        
        return sentMessages.Select(msg => msg.Photo != null ? (nameof(InputMediaPhoto), msg.Photo[0].FileId)
                                         : (nameof(InputMediaVideo), msg.Video!.FileId)
                                         // ... и для других типов ...
                                         ).ToList();
    }
    
    private async Task ForwardFromStorage(ITelegramBotClient botClient, long recipientChatId, List<(string Kind, string FileId)> uploadedMedia, string caption, CancellationToken cancellationToken)
    {
        var mediaGroup = new List<IAlbumInputMedia>();
        foreach (var m in uploadedMedia)
        {
            // --- ИСПРАВЛЕНИЕ ОШИБКИ NULLABILITY ---
            IAlbumInputMedia? media = m.Kind switch
            {
                nameof(InputMediaPhoto) => new InputMediaPhoto(m.FileId),
                nameof(InputMediaVideo) => new InputMediaVideo(m.FileId),
                _ => null
            };
            if (media != null) mediaGroup.Add(media);
        }

        if (!mediaGroup.Any()) return;

        // Прикрепляем подпись к первому элементу
        if (mediaGroup[0] is InputMediaPhoto photo) photo.Caption = caption;
        else if (mediaGroup[0] is InputMediaVideo video) video.Caption = caption;

        await SendInChunks(botClient, recipientChatId, mediaGroup, caption, cancellationToken);
    }

    private async Task<List<Message>> SendInChunks(ITelegramBotClient botClient, long chatId, List<IAlbumInputMedia> media, string? caption, CancellationToken cancellationToken)
    {
        var allSentMessages = new List<Message>();
        for (int i = 0; i < media.Count; i += 10)
        {
            var chunk = media.Skip(i).Take(10).ToList();
            
            // Сбрасываем caption для всех, кроме первого элемента первого чанка
            if (i > 0)
            {
                foreach (var item in chunk)
                {
                    if (item is InputMediaPhoto p) p.Caption = null;
                    else if (item is InputMediaVideo v) v.Caption = null;
                }
            }
            
            try
            {
                var sent = await botClient.SendMediaGroup(chatId, chunk, disableNotification: true, cancellationToken: cancellationToken);
                allSentMessages.AddRange(sent);
            }
            catch (ApiRequestException ex)
            {
                Log.Error(ex, "Failed to send media group chunk to {ChatId}. Fallback might be needed.", chatId);
                // Здесь может быть Fallback на отправку по одному
            }
        }
        return allSentMessages;
    }
}