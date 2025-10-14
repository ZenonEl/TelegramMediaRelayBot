using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToAllContactsCommand : IBotCallbackQueryHandlers
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly MediaDownloaderService _downloaderService;
    private readonly IUserGetter _userGetter;
    private readonly IContactGetter _contactGetter;

    public string Name => "send_to_all_contacts:";
    
    public SendToAllContactsCommand(
        DownloadSessionManager sessionManager, 
        MediaDownloaderService downloaderService, 
        IUserGetter userGetter, 
        IContactGetter contactGetter)
    {
        _sessionManager = sessionManager;
        _downloaderService = downloaderService;
        _userGetter = userGetter;
        _contactGetter = contactGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var callbackQuery = update.CallbackQuery!;
        var messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        
        _sessionManager.CancelDefaultAction(messageId);

        if (!_sessionManager.TryGetSession(messageId, out var session))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Session expired.", true, cancellationToken: ct);
            return;
        }

        var userId = _userGetter.GetUserIDbyTelegramID(session.ChatId);
        var targetTgIds = await _contactGetter.GetAllContactUserTGIds(userId);
        
        await botClient.EditMessageText(session.ChatId, messageId, $"Downloading for all ({targetTgIds.Count}) contacts...", cancellationToken: ct);
        
        // TODO: Передать targetTgIds в процесс отправки после скачивания.
        _ = _downloaderService.DownloadMedia(session.Url, new DownloadOptions(), session.SessionCts.Token);
    }
}