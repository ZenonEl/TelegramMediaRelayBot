using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendOnlyToMeCommand : IBotCallbackQueryHandlers
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly IMediaProcessingFlow _mediaFlow;

    public string Name => "send_only_to_me:";

    public SendOnlyToMeCommand(DownloadSessionManager sessionManager, IMediaProcessingFlow mediaFlow)
    {
        _sessionManager = sessionManager;
        _mediaFlow = mediaFlow;
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

        // Запускаем весь конвейер в фоновом режиме и забываем о нем.
        // Передаем null в targetUserIds, что означает "только себе".
        _ = _mediaFlow.StartFlow(botClient, session, null);
        
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}