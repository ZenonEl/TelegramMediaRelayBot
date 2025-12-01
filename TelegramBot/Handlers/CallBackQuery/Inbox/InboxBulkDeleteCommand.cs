// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.TelegramBot.Validation;

public class InboxBulkDeleteCommand : IBotCallbackQueryHandlers
{
    private readonly IUiResourceService _uiResources;
    private readonly IInboxResourceService _inboxResources;
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    public string Name => "inbox:bulkdel:";
    public InboxBulkDeleteCommand(IInboxRepository inbox,
                                IUserGetter userGetter,
                                IResourceService resourceService,
                                IUiResourceService uiResources,
                                IInboxResourceService inboxResources)
    {
        _inbox = inbox;
        _inboxResources = inboxResources;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _uiResources = uiResources;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // inbox:bulkdel:read|unread:filter:page or inbox:bulkdel:confirm:which:filter:page
        string[] parts = update.CallbackQuery!.Data!.Split(':');
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        if (parts.Length >= 3 && parts[2] == "confirm")
        {
            string which = parts.ElementAtOrDefault(3) ?? "read";
            string filter = parts.ElementAtOrDefault(4) ?? string.Empty;
            int page = parts.Length >= 6 && int.TryParse(parts[5], out int p) ? p : 1;
            string status = which == "read" ? "viewed" : "new";
            await _inbox.DeleteForOwnerAsync(userId, status).ConfigureAwait(false);
            update.CallbackQuery!.Data = $"inbox:list:{page}:{filter}".TrimEnd(':');
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, _uiResources.GetString("UI.Success"), cancellationToken: ct).ConfigureAwait(false);
            await new InboxListCommand(_userGetter, _inbox, _resourceService, new InboxListRequestValidator(), _inboxResources).ExecuteAsync(update, botClient, ct);
            return;
        }
        string target = parts[2];
        string f = parts.ElementAtOrDefault(3) ?? string.Empty;
        int curPage = parts.Length >= 5 && int.TryParse(parts[4], out int pg) ? pg : 1;
        InlineKeyboardMarkup kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(_uiResources.GetString("UI.Button.Yes"), $"inbox:bulkdel:confirm:{target}:{f}:{curPage}") },
            new[] { InlineKeyboardButton.WithCallbackData(_uiResources.GetString("UI.Button.No"), $"inbox:list:{curPage}:{f}".TrimEnd(':')) }
        });
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, _uiResources.GetString("UI.ConfirmDecision"), replyMarkup: kb, cancellationToken: ct).ConfigureAwait(false);
    }
}
