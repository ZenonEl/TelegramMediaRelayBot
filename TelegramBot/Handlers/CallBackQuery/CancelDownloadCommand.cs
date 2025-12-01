// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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
        CallbackQuery callbackQuery = update.CallbackQuery!;
        string[] parts = callbackQuery.Data!.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[^1], out int msgId))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
            return;
        }

        // Вместо TGBot.StateManager... вызываем наш новый менеджер
        bool cancelled = _sessionManager.CancelSession(msgId);

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
