using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Database.Interfaces; // Для IUserGetter, IContactGetter

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToAllCommand : IBotCallbackQueryHandlers
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly MediaDownloaderService _downloaderService;
    private readonly IUserGetter _userGetter;
    private readonly IContactGetter _contactGetter;

    public SendToAllCommand(
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
    
    // Команда будет реагировать на 'send_to_all_contacts:<message_id>'
    public string Name => "send_to_all_contacts";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var callbackQuery = update.CallbackQuery!;
        var messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);

        // 1. Отменяем таймер действия по умолчанию
        _sessionManager.CancelDefaultAction(messageId);

        // 2. Получаем сессию
        if (!_sessionManager.TryGetSession(messageId, out var session))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "This session has expired.", true, cancellationToken: ct);
            return;
        }

        // 3. Подготавливаем цели (все контакты)
        var userId = _userGetter.GetUserIDbyTelegramID(session.ChatId);
        var targetTgIds = await _contactGetter.GetAllContactUserTGIds(userId);
        
        // 4. Запускаем загрузку через MediaDownloaderService
        // TODO: Передать цели (targetTgIds) в HandleMediaRequest
        // _ = _tgBot.HandleMediaRequest(..., targetUserIds: targetTgIds, ...);
        
        _sessionManager.CompleteSession(messageId);
        await botClient.EditMessageText(session.ChatId, messageId, "Starting download for all contacts...", cancellationToken: ct);
    }
}