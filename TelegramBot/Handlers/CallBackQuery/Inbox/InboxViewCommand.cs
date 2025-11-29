// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using FluentValidation;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.TelegramBot.Validation;

public class InboxViewCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    private readonly IResourceService _resourceService;
    public string Name => "inbox:view:";
    private readonly IValidator<InboxViewRequest> _viewValidator;
    public InboxViewCommand(IUserGetter userGetter, IInboxRepository inbox, IValidator<InboxViewRequest> viewValidator, IResourceService resourceService)
    {
        _userGetter = userGetter;
        _inbox = inbox;
        _viewValidator = viewValidator;
        _resourceService = resourceService;
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
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, _resourceService.GetResourceString("InboxItemNotFound"), cancellationToken: ct).ConfigureAwait(false);
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
        string info = string.Format(_resourceService.GetResourceString("Inbox.ViewInfoTemplate"), senderName, h1, h2, captionEsc);
        var kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("InboxDeleteButtonText"), $"inbox:delete:{id}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("BackButtonText"), $"inbox:list:{page}") }
        });
        // Сначала отправляем текст с данными отправителя
        await botClient.SendMessage(chatId, info, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html).ConfigureAwait(false);
        // Затем отдельным сообщением отправляем инлайн-кнопки
        await botClient.SendMessage(chatId, "ㅤ", replyMarkup: kb, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html).ConfigureAwait(false);
    }
}
