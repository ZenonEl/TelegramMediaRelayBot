// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Globalization;
using FluentValidation;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Validation;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class InboxListCommand : IBotCallbackQueryHandlers
{
    private readonly IInboxResourceService _inboxResources;
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:list:";
    private readonly IResourceService _resourceService;
    private readonly IValidator<InboxListRequest> _listValidator;
    private const int PageSize = 10;
    public InboxListCommand(IUserGetter userGetter, IInboxRepository inbox, IResourceService resourceService, IValidator<InboxListRequest> listValidator, IInboxResourceService inboxResources)
    {
        _inboxResources = inboxResources;
        _userGetter = userGetter;
        _inbox = inbox;
        _listValidator = listValidator;
        _resourceService = resourceService;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        string data = update.CallbackQuery!.Data!;
        string[] parts = data.Split(':');
        int page = parts.Length >= 3 && int.TryParse(parts[2], out int p) ? Math.Max(1, p) : 1;
        string filter = parts.Length >= 4 ? parts[3] : string.Empty; // "unread" | "sender" | "sender_unread" | ""
        int? fromSenderId = null;
        if (string.Equals(filter, "sender", StringComparison.OrdinalIgnoreCase) || string.Equals(filter, "sender_unread", StringComparison.OrdinalIgnoreCase))
        {
            // format: inbox:list:<page>:sender[:senderId] OR inbox:list:<page>:sender_unread:<senderId>
            if (parts.Length >= 5 && int.TryParse(parts[4], out int sid)) fromSenderId = sid;
        }
        int pageSize = PageSize;
        FluentValidation.Results.ValidationResult validation = await _listValidator.ValidateAsync(new InboxListRequest { ChatId = chatId, Page = page, PageSize = pageSize }, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            string err = string.Join("\n", validation.Errors.Select(e => $"• {e.ErrorMessage}"));
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, err, showAlert: true, cancellationToken: ct).ConfigureAwait(false);
            return;
        }
        int offset = (page - 1) * pageSize;
        bool onlyUnread = string.Equals(filter, "unread", StringComparison.OrdinalIgnoreCase) || string.Equals(filter, "sender_unread", StringComparison.OrdinalIgnoreCase);
        List<InboxItemDto> items = (await _inbox.GetItemsAsync(userId, onlyUnread ? "new" : null, fromSenderId, pageSize, offset).ConfigureAwait(false)).ToList();
        if (items.Count == 0 && page > 1)
        {
            // fallback to previous page
            page = 1; offset = 0; items = (await _inbox.GetItemsAsync(userId, pageSize, offset).ConfigureAwait(false)).ToList();
        }
        // Build keyboard and header
        List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
        // Filter toggle row + Senders menu
        // Верхняя строка: переключатель Все/Непрочитанные. Если активен просмотр конкретного отправителя,
        // сохраняем его в колбэке, чтобы фильтр работал в рамках отправителя.
        string toggleText = onlyUnread ? _resourceService.GetResourceString("Inbox.Filter.Unread") : _resourceService.GetResourceString("Inbox.Filter.All");
        string toggleCb;
        if (fromSenderId.HasValue)
        {
            toggleCb = onlyUnread ? $"inbox:list:{page}:sender:{fromSenderId.Value}" : $"inbox:list:{page}:sender_unread:{fromSenderId.Value}";
        }
        else
        {
            toggleCb = onlyUnread ? $"inbox:list:{page}" : $"inbox:list:{page}:unread";
        }
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(toggleText, toggleCb), InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.SendersButton"), "inbox:senders:") });
        if (items.Count > 0)
        {
            foreach (InboxItemDto? it in items)
            {
                bool isNew = string.Equals(it.Status, "new", StringComparison.OrdinalIgnoreCase);
                string senderName = _userGetter != null ? _userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(it.FromContactId)) : it.FromContactId.ToString();
                // Try read original time from payload
                DateTime displayTimeLocal;
                try
                {
                    System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(it.PayloadJson);
                    if (doc.RootElement.TryGetProperty("OriginalMessageDateUtc", out System.Text.Json.JsonElement ts) && ts.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        string? s = ts.GetString();
                        if (!string.IsNullOrEmpty(s))
                        {
                            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset dto))
                            {
                                displayTimeLocal = dto.LocalDateTime;
                            }
                            else if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime dt))
                            {
                                displayTimeLocal = DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
                            }
                            else
                            {
                                displayTimeLocal = DateTime.SpecifyKind(it.CreatedAt, DateTimeKind.Utc).ToLocalTime();
                            }
                        }
                        else
                        {
                            displayTimeLocal = DateTime.SpecifyKind(it.CreatedAt, DateTimeKind.Utc).ToLocalTime();
                        }
                    }
                    else
                    {
                        displayTimeLocal = DateTime.SpecifyKind(it.CreatedAt, DateTimeKind.Utc).ToLocalTime();
                    }
                }
                catch
                {
                    displayTimeLocal = DateTime.SpecifyKind(it.CreatedAt, DateTimeKind.Utc).ToLocalTime();
                }
                string title = $"от {senderName} · {displayTimeLocal:yyyy-MM-dd HH:mm}{(isNew ? " · NEW" : " · ✓")}";
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(title, $"inbox:view:{it.Id}:{page}") });
            }
            bool hasPrev = page > 1;
            bool hasNext = items.Count == pageSize;
            if (hasPrev || hasNext)
            {
                string baseFilter = filter;
                if (fromSenderId.HasValue && (string.Equals(filter, "sender", StringComparison.OrdinalIgnoreCase) || string.Equals(filter, "sender_unread", StringComparison.OrdinalIgnoreCase)))
                {
                    baseFilter = $"{filter}:{fromSenderId.Value}";
                }
                string prevCb = hasPrev ? $"inbox:list:{page - 1}:{baseFilter}".TrimEnd(':') : $"inbox:list:{page}:{baseFilter}".TrimEnd(':');
                string nextCb = hasNext ? $"inbox:list:{page + 1}:{baseFilter}".TrimEnd(':') : $"inbox:list:{page}:{baseFilter}".TrimEnd(':');
                List<InlineKeyboardButton> navRow = new List<InlineKeyboardButton>();
                if (hasPrev) navRow.Add(InlineKeyboardButton.WithCallbackData($"◀ {page - 1}", prevCb));
                if (hasNext) navRow.Add(InlineKeyboardButton.WithCallbackData($"▶ {page + 1}", nextCb));
                if (navRow.Count > 0) rows.Add(navRow.ToArray());
            }
            // Bulk actions row
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.MarkAllRead"), $"inbox:mark:read:{filter}:{page}"), InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.MarkAllUnread"), $"inbox:mark:unread:{filter}:{page}") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.DeleteRead"), $"inbox:bulkdel:read:{filter}:{page}"), InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.DeleteUnread"), $"inbox:bulkdel:unread:{filter}:{page}") });
        }
        rows.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });
        InlineKeyboardMarkup kb = new InlineKeyboardMarkup(rows);
        string header = items.Count == 0 ? _inboxResources.GetString("Inbox.Filter.All") : string.Format(_inboxResources.GetString("Inbox.List.Header.Page"), page);
        // Важно всегда пытаться обновлять клавиатуру, даже если заголовок не изменился,
        // иначе переключение фильтра (Все/Непрочитанные) визуально не обновится.
        try
        {
            await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, header, replyMarkup: kb, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
        {
            // Совсем нет изменений — просто игнорируем
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
