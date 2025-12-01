// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class InboxSendersCommand : IBotCallbackQueryHandlers
{
    private readonly IUiResourceService _uiResources;
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    public string Name => "inbox:senders:";
    public InboxSendersCommand(IUserGetter userGetter, IInboxRepository inbox, IUiResourceService uiResources) 
    { _userGetter = userGetter;
    _inbox = inbox;
    _uiResources = uiResources;}
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        List<InboxSenderInfo> senders = (await _inbox.GetSendersAsync(userId).ConfigureAwait(false)).ToList();
        List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
        foreach (InboxSenderInfo? s in senders)
        {
            string name = _userGetter.GetUserNameByTelegramID(_userGetter.GetTelegramIDbyUserID(s.FromContactId));
            string text = $"{name} · {s.NewCount}/{s.Total}";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(text, $"inbox:senderops:{s.FromContactId}:1") });
        }
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData(_uiResources.GetString("UI.Button.BackToMenu"), "inbox:list:1") });
        InlineKeyboardMarkup kb = new InlineKeyboardMarkup(rows);
        await botClient.EditMessageText(chatId, update.CallbackQuery!.Message!.MessageId, _uiResources.GetString("UI.ChooseOption"), replyMarkup: kb, cancellationToken: ct).ConfigureAwait(false);
    }
}
