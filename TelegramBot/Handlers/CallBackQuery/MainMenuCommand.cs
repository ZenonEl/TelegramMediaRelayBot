// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class MainMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "main_menu";

    // Новые, чистые зависимости
    private readonly DownloadSessionManager _sessionManager;
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    private readonly ITelegramInteractionService _interactionService;

    public MainMenuCommand(
        DownloadSessionManager sessionManager,
        IInboxRepository inbox,
        IUserGetter userGetter,
        ITelegramInteractionService interactionService)
    {
        _sessionManager = sessionManager;
        _inbox = inbox;
        _userGetter = userGetter;
        _interactionService = interactionService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;

        // Вместо TGBot.StateManager.TryGet... мы вызываем наш новый менеджер
        // TODO: Реализовать в менеджере метод для отмены ВСЕХ сессий для пользователя
        // _sessionManager.CancelAllForChat(chatId);

        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        int newCount = await _inbox.GetNewCountAsync(userId);
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(newCount), ct);
    }
}
