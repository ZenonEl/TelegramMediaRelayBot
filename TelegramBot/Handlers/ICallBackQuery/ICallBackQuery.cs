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

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public interface IBotCallbackQueryHandlers
{
    string Name { get; }
    Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct);
}

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
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
    }
}

public class OpenInboxCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "open_inbox";
    public OpenInboxCommand(IUserGetter userGetter, IInboxRepository inbox)
    {
        _userGetter = userGetter;
        _inbox = inbox;
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // Show first page via list command
        update.CallbackQuery!.Data = "inbox:list:1";
        await new InboxListCommand(_userGetter, _inbox).ExecuteAsync(update, botClient, ct);
    }
}

public class InboxListCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:list:";
    private readonly IValidator<InboxListRequest> _listValidator;
    public InboxListCommand(IUserGetter userGetter, IInboxRepository inbox)
    {
        _userGetter = userGetter;
        _inbox = inbox;
        _listValidator = new InboxListRequestValidator();
    }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        string data = update.CallbackQuery!.Data!;
        var parts = data.Split(':');
        int page = parts.Length >= 3 && int.TryParse(parts[2], out var p) ? Math.Max(1, p) : 1;
        const int pageSize = 10;
        var validation = await _listValidator.ValidateAsync(new InboxListRequest { ChatId = chatId, Page = page, PageSize = pageSize }, ct);
        if (!validation.IsValid)
        {
            string err = string.Join("\n", validation.Errors.Select(e => $"• {e.ErrorMessage}"));
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, err, showAlert: true, cancellationToken: ct);
            return;
        }
        int offset = (page - 1) * pageSize;
        var items = (await _inbox.GetItemsAsync(userId, pageSize, offset)).ToList();
        if (items.Count == 0 && page > 1)
        {
            // fallback to previous page
            page = 1; offset = 0; items = (await _inbox.GetItemsAsync(userId, pageSize, offset)).ToList();
        }
        // Build keyboard and header
        var rows = new List<InlineKeyboardButton[]>();
        if (items.Count > 0)
        {
            foreach (var it in items)
            {
                bool isNew = string.Equals(it.Status, "new", StringComparison.OrdinalIgnoreCase);
                string senderName = _userGetter != null ? _userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(it.FromContactId)) : it.FromContactId.ToString();
                string title = $"#{it.Id} · от {senderName} · {it.CreatedAt:yyyy-MM-dd HH:mm}{(isNew ? " · NEW" : " · ✓")}";
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(title, $"inbox:view:{it.Id}:{page}") });
            }
            bool hasPrev = page > 1;
            bool hasNext = items.Count == pageSize;
            if (hasPrev || hasNext)
            {
                var prevCb = hasPrev ? $"inbox:list:{page - 1}" : $"inbox:list:{page}";
                var nextCb = hasNext ? $"inbox:list:{page + 1}" : $"inbox:list:{page}";
                var navRow = new List<InlineKeyboardButton>();
                if (hasPrev) navRow.Add(InlineKeyboardButton.WithCallbackData($"◀ {page - 1}", prevCb));
                if (hasNext) navRow.Add(InlineKeyboardButton.WithCallbackData($"▶ {page + 1}", nextCb));
                if (navRow.Count > 0) rows.Add(navRow.ToArray());
            }
        }
        rows.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });
        var kb = new InlineKeyboardMarkup(rows);
        string header = items.Count == 0 ? "Ваш список входящих\nПока пусто" : $"Ваш список входящих\nТекущий лист: {page}";
        string currentText = update.CallbackQuery!.Message!.Text ?? string.Empty;
        if (string.Equals(currentText, header, StringComparison.Ordinal))
        {
            // Ничего менять не нужно
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct);
            return;
        }
        try
        {
            await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, header, replyMarkup: kb, cancellationToken: ct);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
        {
            // Совсем нет изменений — просто игнорируем
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct);
        }
    }
}

public class InboxViewCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:view:";
    private readonly IValidator<InboxViewRequest> _viewValidator;
    public InboxViewCommand(IUserGetter userGetter, IInboxRepository inbox)
    {
        _userGetter = userGetter;
        _inbox = inbox;
        _viewValidator = new InboxViewRequestValidator();
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
        var validation = await _viewValidator.ValidateAsync(new InboxViewRequest { ChatId = chatId, ItemId = id, Page = page }, ct);
        if (!validation.IsValid)
        {
            string err = string.Join("\n", validation.Errors.Select(e => $"• {e.ErrorMessage}"));
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, err, showAlert: true, cancellationToken: ct);
            return;
        }
        var item = await _inbox.GetItemAsync(id);
        if (item == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, "Не найдено", cancellationToken: ct);
            return;
        }
        var payload = System.Text.Json.JsonSerializer.Deserialize<Payload>(item.PayloadJson) ?? new Payload();
        // Rebuild media group from file ids
        var media = new List<IAlbumInputMedia>();
        foreach (var m in payload.SavedMedia)
        {
            string t = m.Type?.ToLowerInvariant() ?? string.Empty;
            if (t.Contains("photo") || t == "image") { media.Add(new InputMediaPhoto(m.FileId) { Caption = m.Caption }); continue; }
            if (t.Contains("video")) { media.Add(new InputMediaVideo(m.FileId) { Caption = m.Caption }); continue; }
            if (t.Contains("audio")) { media.Add(new InputMediaAudio(m.FileId) { Caption = m.Caption }); continue; }
            media.Add(new InputMediaDocument(m.FileId) { Caption = m.Caption });
        }
        if (media.Count > 0)
        {
            await botClient.SendMediaGroup(chatId, media, disableNotification: true, cancellationToken: ct);
        }
        await _inbox.SetStatusAsync(id, "viewed");
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
        string info = $"От: {senderName}\n<code>{h1}</code>\n#{h2}\n\n{captionEsc}";
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Удалить", $"inbox:delete:{id}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData("Назад", $"inbox:list:{page}") }
        });
        // Сначала отправляем текст с данными отправителя
        await botClient.SendMessage(chatId, info, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        // Затем отдельным сообщением отправляем инлайн-кнопки
        await botClient.SendMessage(chatId, "ㅤ", replyMarkup: kb, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
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
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct);
            return;
        }
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        if (TGBot.StateManager.TryGet(chatId, out var state) && state is ProcessVideoDC s)
        {
            // Пытаемся отменить только конкретную сессию отправки по messageId
            s.DisableAutoForMessageId(msgId);
            // Используем внутреннее хранилище токенов сессии
            var field = typeof(ProcessVideoDC).GetField("_sessionCtsByMessageId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(s) is Dictionary<int, CancellationTokenSource> map && map.TryGetValue(msgId, out var cts))
            {
                try { cts.Cancel(); } catch { }
                map.Remove(msgId);
            }
        }
        await botClient.EditMessageText(update.CallbackQuery!.Message!.Chat.Id, update.CallbackQuery!.Message!.MessageId, "❎ Отменено пользователем", cancellationToken: ct);
    }
}

public class InboxDeleteCommand : IBotCallbackQueryHandlers
{
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    public string Name => "inbox:delete:";
    public InboxDeleteCommand(IInboxRepository inbox, IUserGetter userGetter) { _inbox = inbox; _userGetter = userGetter; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var parts = update.CallbackQuery!.Data!.Split(':');
        long id = long.Parse(parts[2]);
        int page = parts.Length >= 4 && int.TryParse(parts[3], out var p) ? p : 1;
        await _inbox.DeleteAsync(id);
        // go back to list
        update.CallbackQuery!.Data = $"inbox:list:{page}";
        await new InboxListCommand(_userGetter, _inbox).ExecuteAsync(update, botClient, ct);
    }
}