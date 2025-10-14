using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendOnlyToMeCommand : IBotCallbackQueryHandlers
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly MediaDownloaderService _downloaderService;
    // ... возможно, понадобится ITelegramInteractionService для отправки финального сообщения

    public string Name => "send_only_to_me:";

    public SendOnlyToMeCommand(
        DownloadSessionManager sessionManager,
        MediaDownloaderService downloaderService)
    {
        _sessionManager = sessionManager;
        _downloaderService = downloaderService;
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

        await botClient.EditMessageText(session.ChatId, messageId, "Downloading for you...", cancellationToken: ct);

        // Запускаем загрузку без списка целей (что означает "только себе")
        _ = _downloaderService.DownloadMedia(session.Url, new DownloadOptions(), session.SessionCts.Token);
        // Мы не ждем (await) завершения, чтобы бот мог принимать другие команды.
        // TODO: Всю логику после скачивания (отправка, обработка ошибок) нужно будет вызвать по завершению DownloadMedia.
    }
}