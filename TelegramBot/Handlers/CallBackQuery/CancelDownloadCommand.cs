using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class CancelDownloadCommand : IBotCallbackQueryHandlers
{
    public string Name => "cancel_download:";
    
    private readonly DownloadSessionManager _sessionManager;
    private readonly Config.Services.IResourceService _resourceService;

    public CancelDownloadCommand(DownloadSessionManager sessionManager, Config.Services.IResourceService resourceService)
    {
        _sessionManager = sessionManager;
        _resourceService = resourceService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var callbackQuery = update.CallbackQuery!;
        var parts = callbackQuery.Data!.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[^1], out var msgId))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
            return;
        }

        // Вместо TGBot.StateManager... вызываем наш новый менеджер
        var cancelled = _sessionManager.CancelSession(msgId);

        if (cancelled)
        {
            try
            {
                // Попробуем отредактировать сообщение, от которого пришел колбек
                if (callbackQuery.Message != null)
                {
                    await botClient.EditMessageText(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, 
                        _resourceService.GetResourceString("CanceledByUserMessage"), cancellationToken: ct);
                }
            }
            catch { /* Игнорируем ошибки, если сообщение уже удалено и т.д. */ }
        }
        else
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, 
                _resourceService.GetResourceString("NothingToCancelMessage"), showAlert: false, cancellationToken: ct);
        }
    }
}