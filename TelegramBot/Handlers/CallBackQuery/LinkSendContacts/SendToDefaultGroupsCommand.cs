using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToDefaultGroupsCommand : IBotCallbackQueryHandlers
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly MediaDownloaderService _downloaderService;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;

    public string Name => "send_to_default_groups:";
    
    public SendToDefaultGroupsCommand(
        DownloadSessionManager sessionManager, 
        MediaDownloaderService downloaderService, 
        IUserGetter userGetter, 
        IGroupGetter groupGetter)
    {
        _sessionManager = sessionManager;
        _downloaderService = downloaderService;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
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
        // Получаем ID всех пользователей во всех группах по умолчанию
        var userIdsInGroups = await _groupGetter.GetAllUsersInDefaultEnabledGroups(userId);
        var targetTgIds = userIdsInGroups.Select(id => _userGetter.GetTelegramIDbyUserID(id)).ToList();
        
        await botClient.EditMessageText(session.ChatId, messageId, $"Downloading for default groups ({targetTgIds.Count} users)...", cancellationToken: ct);
        
        // TODO: Передать targetTgIds в процесс отправки после скачивания.
        _ = _downloaderService.DownloadMedia(session.Url, new DownloadOptions(), session.SessionCts.Token);
    }
}