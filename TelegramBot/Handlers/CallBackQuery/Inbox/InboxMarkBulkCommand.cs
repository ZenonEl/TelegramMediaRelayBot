// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.TelegramBot.Validation;

public class InboxMarkBulkCommand : IBotCallbackQueryHandlers
{
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    public string Name => "inbox:mark:";
    public InboxMarkBulkCommand(IInboxRepository inbox, IUserGetter userGetter, IResourceService resourceService) { _inbox = inbox; _userGetter = userGetter; _resourceService = resourceService; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // inbox:mark:read|unread:filter:page or inbox:mark:confirm:toStatus:filter:page
        var parts = update.CallbackQuery!.Data!.Split(':');
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        if (parts.Length >= 3 && parts[2] == "confirm")
        {
            string to = parts.ElementAtOrDefault(3) ?? "read";
            string filter = parts.ElementAtOrDefault(4) ?? string.Empty;
            int page = parts.Length >= 6 && int.TryParse(parts[5], out var p) ? p : 1;
            string toStatus = to == "read" ? "viewed" : "new";
            string fromStatus = to == "read" ? "new" : "viewed";
            await _inbox.SetStatusForOwnerAsync(userId, fromStatus, toStatus).ConfigureAwait(false);
            update.CallbackQuery!.Data = $"inbox:list:{page}:{filter}".TrimEnd(':');
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, _resourceService.GetResourceString("SuccessActionResult"), cancellationToken: ct).ConfigureAwait(false);
            await new InboxListCommand(_userGetter, _inbox, _resourceService, new InboxListRequestValidator()).ExecuteAsync(update, botClient, ct);
            return;
        }
        string toStatusKey = parts[2];
        string f = parts.ElementAtOrDefault(3) ?? string.Empty;
        int curPage = parts.Length >= 5 && int.TryParse(parts[4], out var pg) ? pg : 1;
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("YesButtonText"), $"inbox:mark:confirm:{toStatusKey}:{f}:{curPage}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("NoButtonText"), $"inbox:list:{curPage}:{f}".TrimEnd(':')) }
        });
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, _resourceService.GetResourceString("ConfirmDecision"), replyMarkup: kb, cancellationToken: ct).ConfigureAwait(false);
    }
}

