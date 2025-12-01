// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class InboxSenderOperationsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _resourceService;
    public string Name => "inbox:senderops:";
    public InboxSenderOperationsCommand(IUserGetter userGetter, IResourceService resourceService)
    { _userGetter = userGetter; _resourceService = resourceService; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string[] parts = update.CallbackQuery!.Data!.Split(':');
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        int senderContactId = int.Parse(parts[2]);
        int page = parts.Length >= 4 && int.TryParse(parts[3], out int p) ? p : 1;
        string name = _userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(senderContactId));
        InlineKeyboardMarkup kb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.Sender.ShowAll"), $"inbox:list:1:sender:{senderContactId}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.Sender.ShowUnread"), $"inbox:list:1:sender_unread:{senderContactId}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.Sender.MarkRead"), $"inbox:sender:mark:confirm:read:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.Sender.MarkUnread"), $"inbox:sender:mark:confirm:unread:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.Sender.DeleteRead"), $"inbox:sender:del:confirm:read:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("Inbox.Sender.DeleteUnread"), $"inbox:sender:del:confirm:unread:{senderContactId}:{page}") },
            new[] { InlineKeyboardButton.WithCallbackData(_resourceService.GetResourceString("BackButtonText"), "inbox:senders:") }
        });
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, System.Net.WebUtility.HtmlEncode(name), replyMarkup: kb, cancellationToken: ct, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html).ConfigureAwait(false);
    }
}

