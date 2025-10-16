using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendOnlyToMeCommand : IBotCallbackQueryHandlers
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly IServiceScopeFactory _scopeFactory;

    public string Name => "send_only_to_me:";

    public SendOnlyToMeCommand(DownloadSessionManager sessionManager, IServiceScopeFactory scopeFactory)
    {
        _sessionManager = sessionManager;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        CallbackQuery callbackQuery = update.CallbackQuery!;
        int messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        
        _sessionManager.CancelDefaultAction(messageId);

        if (!_sessionManager.TryGetSession(messageId, out DownloadSession? session))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Session expired.", true, cancellationToken: ct);
            return;
        }

        await botClient.EditMessageText(session.ChatId, messageId, "Processing...", cancellationToken: ct);
        
        // --- ГЛАВНОЕ ИСПРАВЛЕНИЕ ---
        // Запускаем фоновую задачу, которая СОЗДАЕТ СВОЙ SCOPE
        _ = Task.Run(async () =>
        {
            // 1. Создаем свой собственный, независимый scope
            await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
            {
                // 2. Получаем "свежий" IMediaProcessingFlow из этого scope
                IMediaProcessingFlow mediaFlow = scope.ServiceProvider.GetRequiredService<IMediaProcessingFlow>();
                
                // 3. Выполняем всю долгую операцию внутри scope
                await mediaFlow.StartFlow(botClient, session, null);
            }
        }, session.SessionCts.Token); // Используем CancellationToken из сессии
        
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}