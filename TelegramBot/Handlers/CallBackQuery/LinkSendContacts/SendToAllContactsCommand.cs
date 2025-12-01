// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToAllContactsCommand : IBotCallbackQueryHandlers
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUserGetter _userGetter;
    private readonly IContactGetter _contactGetter;

    public string Name => "send_to_all_contacts:";

    public SendToAllContactsCommand(
        DownloadSessionManager sessionManager,
        IServiceScopeFactory scopeFactory,
        IUserGetter userGetter,
        IContactGetter contactGetter)
    {
        _sessionManager = sessionManager;
        _scopeFactory = scopeFactory;
        _userGetter = userGetter;
        _contactGetter = contactGetter;
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

        int userId = _userGetter.GetUserIDbyTelegramID(session.ChatId);
        List<long> allContactTgIds = await _contactGetter.GetAllContactUserTGIds(userId);

        _sessionManager.MarkAsProcessing(messageId);
        await botClient.EditMessageText(session.ChatId, messageId,
            $"Starting distribution to all contacts ({allContactTgIds.Count})...",
            cancellationToken: ct);

        _ = Task.Run(async () =>
        {
            await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
            {
                IMediaProcessingFlow mediaFlow = scope.ServiceProvider.GetRequiredService<IMediaProcessingFlow>();
                await mediaFlow.StartFlow(botClient, update, session, allContactTgIds);
            }
        }, session.SessionCts.Token);
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}
