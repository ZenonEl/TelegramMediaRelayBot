// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.Database.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;
using FluentValidation;
using TelegramMediaRelayBot.TelegramBot.Validation;
using TelegramMediaRelayBot.TelegramBot.Menu;
using System.Globalization;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

/// <summary>
/// Defines a Telegram callback query handler.
/// Implementations handle a specific callback command identified by <see cref="Name"/>.
/// </summary>
public interface IBotCallbackQueryHandlers
{
    /// <summary>
    /// Unique prefix or full name of the callback command this handler processes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the handler logic for the provided callback update.
    /// </summary>
    Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct);

public class MainMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "main_menu";
    private readonly TelegramMediaRelayBot.Database.Interfaces.IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    public MainMenuCommand(TelegramMediaRelayBot.Database.Interfaces.IInboxRepository inbox, IUserGetter userGetter)
    {
        _inbox = inbox;
        _userGetter = userGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        if (TGBot.StateManager.TryGet(chatId, out var state) && state is ProcessVideoDC s)
        {
            s.CancelAll();
            TGBot.StateManager.Remove(chatId);
        }
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        int newCount = await _inbox.GetNewCountAsync(userId);
        await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, ct, inboxNewCount: newCount);
    }
}

public class GetSelfLinkCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    public string Name => "get_self_link";

    public GetSelfLinkCommand(IUserGetter userGetter)
    {
        _userGetter = userGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CallbackQueryMenuUtils.GetSelfLink(botClient, update, _userGetter);
    }
}

public class WhosTheGeniusCommand : IBotCallbackQueryHandlers
{
    public string Name => "whos_the_genius";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CallbackQueryMenuUtils.WhosTheGenius(botClient, update);
    }
}

public class ShowHelpCommand : IBotCallbackQueryHandlers
{
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public ShowHelpCommand(TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public string Name => "show_help";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string helpText = _resourceService.GetResourceString("HelpText");
        await botClient.EditMessageText(
            chatId: update.CallbackQuery!.Message!.Chat.Id,
            messageId: update.CallbackQuery!.Message!.MessageId,
            text: helpText,
            replyMarkup: KeyboardUtils.GetReturnButtonMarkup("main_menu"),
            cancellationToken: ct,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html).ConfigureAwait(false);
    }
}

public class OpenInboxCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "open_inbox";
    private readonly IValidator<InboxListRequest> _listValidator;
    public OpenInboxCommand(IUserGetter userGetter, IInboxRepository inbox, IValidator<InboxListRequest> listValidator)
    {
        _userGetter = userGetter;
        _inbox = inbox;
        _listValidator = listValidator;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // Show first page via list command
        update.CallbackQuery!.Data = "inbox:list:1";
        await new InboxListCommand(_userGetter, _inbox, _listValidator).ExecuteAsync(update, botClient, ct);
    }
}

public class InboxListCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:list:";
    private readonly IValidator<InboxListRequest> _listValidator;
    private const int PageSize = 10;
    public InboxListCommand(IUserGetter userGetter, IInboxRepository inbox, IValidator<InboxListRequest> listValidator)
    {
        _userGetter = userGetter;
        _inbox = inbox;
        _listValidator = listValidator;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        string data = update.CallbackQuery!.Data!;
        var parts = data.Split(':');
        int page = parts.Length >= 3 && int.TryParse(parts[2], out var p) ? Math.Max(1, p) : 1;
        string filter = parts.Length >= 4 ? parts[3] : string.Empty; // "unread" | "sender" | "sender_unread" | ""
        int? fromSenderId = null;
        if (string.Equals(filter, "sender", StringComparison.OrdinalIgnoreCase) || string.Equals(filter, "sender_unread", StringComparison.OrdinalIgnoreCase))
        {
            // format: inbox:list:<page>:sender[:senderId] OR inbox:list:<page>:sender_unread:<senderId>
            if (parts.Length >= 5 && int.TryParse(parts[4], out var sid)) fromSenderId = sid;
        }
        int pageSize = PageSize;
        var validation = await _listValidator.ValidateAsync(new InboxListRequest { ChatId = chatId, Page = page, PageSize = pageSize }, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            string err = string.Join("\n", validation.Errors.Select(e => $"• {e.ErrorMessage}"));
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, err, showAlert: true, cancellationToken: ct).ConfigureAwait(false);
            return;
        }
        int offset = (page - 1) * pageSize;
        bool onlyUnread = string.Equals(filter, "unread", StringComparison.OrdinalIgnoreCase) || string.Equals(filter, "sender_unread", StringComparison.OrdinalIgnoreCase);
        var items = (await _inbox.GetItemsAsync(userId, onlyUnread ? "new" : null, fromSenderId, pageSize, offset).ConfigureAwait(false)).ToList();
        if (items.Count == 0 && page > 1)
        {
            // fallback to previous page
            page = 1; offset = 0; items = (await _inbox.GetItemsAsync(userId, pageSize, offset).ConfigureAwait(false)).ToList();
        }
        // Build keyboard and header
        var rows = new List<InlineKeyboardButton[]>();
        // Filter toggle row + Senders menu
        // Верхняя строка: переключатель Все/Непрочитанные. Если активен просмотр конкретного отправителя,
        // сохраняем его в колбэке, чтобы фильтр работал в рамках отправителя.
        string toggleText = onlyUnread ? Users.GetResourceString("Inbox.Filter.Unread") : Users.GetResourceString("Inbox.Filter.All");
        string toggleCb;
        if (fromSenderId.HasValue)
        {
            toggleCb = onlyUnread ? $"inbox:list:{page}:sender:{fromSenderId.Value}" : $"inbox:list:{page}:sender_unread:{fromSenderId.Value}";
        }
        else
        {
            toggleCb = onlyUnread ? $"inbox:list:{page}" : $"inbox:list:{page}:unread";
        }
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(toggleText, toggleCb), InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.SendersButton"), "inbox:senders:") });
        if (items.Count > 0)
        {
            foreach (var it in items)
            {
                bool isNew = string.Equals(it.Status, "new", StringComparison.OrdinalIgnoreCase);
                string senderName = _userGetter != null ? _userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(it.FromContactId)) : it.FromContactId.ToString();
                // Try read original time from payload
                DateTime displayTimeLocal;
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(it.PayloadJson);
                    if (doc.RootElement.TryGetProperty("OriginalMessageDateUtc", out var ts) && ts.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var s = ts.GetString();
                        if (!string.IsNullOrEmpty(s))
                        {
                            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                            {
                                displayTimeLocal = dto.LocalDateTime;
                            }
                            else if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
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
                var prevCb = hasPrev ? $"inbox:list:{page - 1}:{baseFilter}".TrimEnd(':') : $"inbox:list:{page}:{baseFilter}".TrimEnd(':');
                var nextCb = hasNext ? $"inbox:list:{page + 1}:{baseFilter}".TrimEnd(':') : $"inbox:list:{page}:{baseFilter}".TrimEnd(':');
                var navRow = new List<InlineKeyboardButton>();
                if (hasPrev) navRow.Add(InlineKeyboardButton.WithCallbackData($"◀ {page - 1}", prevCb));
                if (hasNext) navRow.Add(InlineKeyboardButton.WithCallbackData($"▶ {page + 1}", nextCb));
                if (navRow.Count > 0) rows.Add(navRow.ToArray());
            }
            // Bulk actions row
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.MarkAllRead"), $"inbox:mark:read:{filter}:{page}"), InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.MarkAllUnread"), $"inbox:mark:unread:{filter}:{page}") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.DeleteRead"), $"inbox:bulkdel:read:{filter}:{page}"), InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.DeleteUnread"), $"inbox:bulkdel:unread:{filter}:{page}") });
        }
        rows.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });
        var kb = new InlineKeyboardMarkup(rows);
        string header = items.Count == 0 ? Users.GetResourceString("InboxListEmpty") : string.Format(Users.GetResourceString("InboxListCurrentPage"), page);
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

public class InboxViewCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:view:";
    private readonly IValidator<InboxViewRequest> _viewValidator;
    public InboxViewCommand(IUserGetter userGetter, IInboxRepository inbox, IValidator<InboxViewRequest> viewValidator)
    {
        _userGetter = userGetter;
        _inbox = inbox;
        _viewValidator = viewValidator;
    }
    private sealed class SavedMediaItem
    {
        public string Type { get; set; } = string.Empty; // serialized name in payload
        public string FileId { get; set; } = string.Empty;
        public string? Caption { get; set; }
    }
    private sealed class Payload
    {
        public long SourceChatId { get; set; }
        public List<SavedMediaItem> SavedMedia { get; set; } = new();
        public string Url { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public object? Hashtag { get; set; }
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        var parts = update.CallbackQuery!.Data!.Split(':');
        long id = long.Parse(parts[2]);
        int page = parts.Length >= 4 && int.TryParse(parts[3], out var p) ? p : 1;
        var validation = await _viewValidator.ValidateAsync(new InboxViewRequest { ChatId = chatId, ItemId = id, Page = page }, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            string err = string.Join("\n", validation.Errors.Select(e => $"• {e.ErrorMessage}"));
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, err, showAlert: true, cancellationToken: ct).ConfigureAwait(false);
            return;
        }
        var item = await _inbox.GetItemAsync(id).ConfigureAwait(false);
        if (item == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, Users.GetResourceString("InboxItemNotFound"), cancellationToken: ct).ConfigureAwait(false);
            return;
        }
        var payload = System.Text.Json.JsonSerializer.Deserialize<Payload>(item.PayloadJson) ?? new Payload();
        // Rebuild media from file ids with type-safe sending
        var photoVideo = new List<IAlbumInputMedia>();
        var audios = new List<string>();
        var documents = new List<string>();
        foreach (var m in payload.SavedMedia)
        {
            string t = m.Type?.ToLowerInvariant() ?? string.Empty;
            if (t.Contains("photo") || t == "image") { photoVideo.Add(new InputMediaPhoto(m.FileId) { Caption = m.Caption }); continue; }
            if (t.Contains("video")) { photoVideo.Add(new InputMediaVideo(m.FileId) { Caption = m.Caption }); continue; }
            if (t.Contains("audio")) { audios.Add(m.FileId); continue; }
            documents.Add(m.FileId);
        }
        if (photoVideo.Count > 0)
        {
            await botClient.SendMediaGroup(chatId, photoVideo, disableNotification: true, cancellationToken: ct).ConfigureAwait(false);
        }
        foreach (var a in audios)
        {
            try { await botClient.SendAudio(chatId, a, cancellationToken: ct).ConfigureAwait(false); } catch { }
        }
        foreach (var d in documents)
        {
            try { await botClient.SendDocument(chatId, d, cancellationToken: ct).ConfigureAwait(false); } catch { }
        }
        await _inbox.SetStatusAsync(id, "viewed").ConfigureAwait(false);
        string senderName = System.Net.WebUtility.HtmlEncode(_userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(item.FromContactId)));
        // Extract two hashes: H1 (code) and H2 (#)
        string h1 = string.Empty;
        string h2 = string.Empty;
        if (payload.Hashtag is string hs)
        {
            h1 = hs;
        }
        else if (payload.Hashtag is System.Text.Json.JsonElement el)
        {
            if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var arr = el.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                h1 = arr.ElementAtOrDefault(0) ?? string.Empty;
                h2 = arr.ElementAtOrDefault(1) ?? string.Empty;
            }
            else if (el.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                h1 = el.GetString() ?? string.Empty;
            }
        }
        h1 = System.Net.WebUtility.HtmlEncode(h1);
        h2 = System.Net.WebUtility.HtmlEncode(h2);
        string captionEsc = System.Net.WebUtility.HtmlEncode(payload.Caption ?? string.Empty);
        string info = string.Format(Users.GetResourceString("Inbox.ViewInfoTemplate"), senderName, h1, h2, captionEsc);
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("InboxDeleteButtonText"), $"inbox:delete:{id}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("BackButtonText"), $"inbox:list:{page}") }
        });
        // Сначала отправляем текст с данными отправителя
        await botClient.SendMessage(chatId, info, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html).ConfigureAwait(false);
        // Затем отдельным сообщением отправляем инлайн-кнопки
        await botClient.SendMessage(chatId, "ㅤ", replyMarkup: kb, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html).ConfigureAwait(false);
    }
}

public class InboxSendersCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:senders:";
    public InboxSendersCommand(IUserGetter userGetter, IInboxRepository inbox) { _userGetter = userGetter; _inbox = inbox; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        var senders = (await _inbox.GetSendersAsync(userId).ConfigureAwait(false)).ToList();
        var rows = new List<InlineKeyboardButton[]>();
        foreach (var s in senders)
        {
            string name = _userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(s.FromContactId));
            string text = $"{name} · {s.NewCount}/{s.Total}";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(text, $"inbox:senderops:{s.FromContactId}:1") });
        }
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("BackButtonText"), "inbox:list:1") });
        var kb = new InlineKeyboardMarkup(rows);
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, Users.GetResourceString("ChooseOptionText"), replyMarkup: kb, cancellationToken: ct).ConfigureAwait(false);
    }
}

public class InboxSenderOperationsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:senderops:";
    public InboxSenderOperationsCommand(IUserGetter userGetter, IInboxRepository inbox) { _userGetter = userGetter; _inbox = inbox; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var parts = update.CallbackQuery!.Data!.Split(':');
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        int senderContactId = int.Parse(parts[2]);
        int page = parts.Length >= 4 && int.TryParse(parts[3], out var p) ? p : 1;
        string name = _userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(senderContactId));
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.Sender.ShowAll"), $"inbox:list:1:sender:{senderContactId}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.Sender.ShowUnread"), $"inbox:list:1:sender_unread:{senderContactId}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.Sender.MarkRead"), $"inbox:sender:mark:confirm:read:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.Sender.MarkUnread"), $"inbox:sender:mark:confirm:unread:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.Sender.DeleteRead"), $"inbox:sender:del:confirm:read:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("Inbox.Sender.DeleteUnread"), $"inbox:sender:del:confirm:unread:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("BackButtonText"), "inbox:senders:") }
        });
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, System.Net.WebUtility.HtmlEncode(name), replyMarkup: kb, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html).ConfigureAwait(false);
    }
}

public class InboxSenderBulkApplyCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:sender:";
    public InboxSenderBulkApplyCommand(IUserGetter userGetter, IInboxRepository inbox) { _userGetter = userGetter; _inbox = inbox; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // inbox:sender:mark:confirm:read|unread:senderId:page OR inbox:sender:del:confirm:read|unread:senderId:page
        var parts = update.CallbackQuery!.Data!.Split(':');
        if (parts.Length < 6) { await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct).ConfigureAwait(false); return; }
        string mode = parts[2]; // mark|del
        string which = parts[4]; // read|unread
        int senderId = int.Parse(parts[5]);
        int page = parts.Length >= 7 && int.TryParse(parts[6], out var p) ? p : 1;
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int ownerId = _userGetter.GetUserIDbyTelegramID(chatId);
        if (mode == "mark")
        {
            string to = which == "read" ? "viewed" : "new";
            string from = which == "read" ? "new" : "viewed";
            await _inbox.SetStatusForOwnerAsync(ownerId, from, to, senderId).ConfigureAwait(false);
        }
        else if (mode == "del")
        {
            string st = which == "read" ? "viewed" : "new";
            await _inbox.DeleteForOwnerAsync(ownerId, st, senderId).ConfigureAwait(false);
        }
        update.CallbackQuery!.Data = $"inbox:senders:";
        await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, Users.GetResourceString("SuccessActionResult"), cancellationToken: ct).ConfigureAwait(false);
        await new InboxSendersCommand(_userGetter, _inbox).ExecuteAsync(update, botClient, ct);
    }
}

public class CancelDownloadCommand : IBotCallbackQueryHandlers
{
    public string Name => "cancel_download:";
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // колбек формата cancel_download:<messageId>
        var parts = update.CallbackQuery!.Data!.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[^1], out var msgId))
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct).ConfigureAwait(false);
            return;
        }
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        bool cancelled = false;
        if (TGBot.StateManager.TryGet(chatId, out var state) && state is ProcessVideoDC s)
        {
            cancelled = s.CancelSessionForMessageId(msgId);
        }
        if (cancelled)
        {
            await botClient.EditMessageText(update.CallbackQuery!.Message!.Chat.Id, update.CallbackQuery!.Message!.MessageId, Users.GetResourceString("CanceledByUserMessage"), cancellationToken: ct).ConfigureAwait(false);
        }
        else
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, Users.GetResourceString("NothingToCancelMessage"), cancellationToken: ct, showAlert: false).ConfigureAwait(false);
        }
    }
}

public class InboxDeleteCommand : IBotCallbackQueryHandlers
{
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    private readonly CallbackQueryHandlersFactory _factory;
    public string Name => "inbox:delete:";
    public InboxDeleteCommand(IInboxRepository inbox, IUserGetter userGetter, CallbackQueryHandlersFactory factory) { _inbox = inbox; _userGetter = userGetter; _factory = factory; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var parts = update.CallbackQuery!.Data!.Split(':');
        long id = long.Parse(parts[2]);
        int page = parts.Length >= 4 && int.TryParse(parts[3], out var p) ? p : 1;
        await _inbox.DeleteAsync(id).ConfigureAwait(false);
        // go back to list
        update.CallbackQuery!.Data = $"inbox:list:{page}";
        await _factory.ExecuteAsync("inbox:list:", update, botClient, ct).ConfigureAwait(false);
    }
}

public class InboxMarkBulkCommand : IBotCallbackQueryHandlers
{
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    public string Name => "inbox:mark:";
    public InboxMarkBulkCommand(IInboxRepository inbox, IUserGetter userGetter) { _inbox = inbox; _userGetter = userGetter; }
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
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, Users.GetResourceString("SuccessActionResult"), cancellationToken: ct).ConfigureAwait(false);
            await new InboxListCommand(_userGetter, _inbox, new InboxListRequestValidator()).ExecuteAsync(update, botClient, ct);
            return;
        }
        string toStatusKey = parts[2];
        string f = parts.ElementAtOrDefault(3) ?? string.Empty;
        int curPage = parts.Length >= 5 && int.TryParse(parts[4], out var pg) ? pg : 1;
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("YesButtonText"), $"inbox:mark:confirm:{toStatusKey}:{f}:{curPage}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("NoButtonText"), $"inbox:list:{curPage}:{f}".TrimEnd(':')) }
        });
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, Users.GetResourceString("ConfirmDecision"), replyMarkup: kb, cancellationToken: ct).ConfigureAwait(false);
    }
}

public class InboxBulkDeleteCommand : IBotCallbackQueryHandlers
{
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    public string Name => "inbox:bulkdel:";
    public InboxBulkDeleteCommand(IInboxRepository inbox, IUserGetter userGetter) { _inbox = inbox; _userGetter = userGetter; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // inbox:bulkdel:read|unread:filter:page or inbox:bulkdel:confirm:which:filter:page
        var parts = update.CallbackQuery!.Data!.Split(':');
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        if (parts.Length >= 3 && parts[2] == "confirm")
        {
            string which = parts.ElementAtOrDefault(3) ?? "read";
            string filter = parts.ElementAtOrDefault(4) ?? string.Empty;
            int page = parts.Length >= 6 && int.TryParse(parts[5], out var p) ? p : 1;
            string status = which == "read" ? "viewed" : "new";
            await _inbox.DeleteForOwnerAsync(userId, status).ConfigureAwait(false);
            update.CallbackQuery!.Data = $"inbox:list:{page}:{filter}".TrimEnd(':');
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, Users.GetResourceString("SuccessActionResult"), cancellationToken: ct).ConfigureAwait(false);
            await new InboxListCommand(_userGetter, _inbox, new InboxListRequestValidator()).ExecuteAsync(update, botClient, ct);
            return;
        }
        string target = parts[2];
        string f = parts.ElementAtOrDefault(3) ?? string.Empty;
        int curPage = parts.Length >= 5 && int.TryParse(parts[4], out var pg) ? pg : 1;
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("YesButtonText"), $"inbox:bulkdel:confirm:{target}:{f}:{curPage}") },
            new[] { InlineKeyboardButton.WithCallbackData(Users.GetResourceString("NoButtonText"), $"inbox:list:{curPage}:{f}".TrimEnd(':')) }
        });
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, Users.GetResourceString("ConfirmDecision"), replyMarkup: kb, cancellationToken: ct).ConfigureAwait(false);
    }
}
}