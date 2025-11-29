// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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

        _sessionManager.MarkAsProcessing(messageId);
        await botClient.EditMessageText(session.ChatId, messageId, "Processing...", cancellationToken: ct);
        
        _ = Task.Run(async () =>
        {
            await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
            {
                IMediaProcessingFlow mediaFlow = scope.ServiceProvider.GetRequiredService<IMediaProcessingFlow>();
                await mediaFlow.StartFlow(botClient, update, session, null);
            }
        }, session.SessionCts.Token);
        
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}