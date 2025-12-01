// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class InboxDeleteCommand : IBotCallbackQueryHandlers
{
    private readonly IInboxRepository _inbox;
    private readonly IUserGetter _userGetter;
    private readonly CallbackQueryHandlersFactory _factory;
    public string Name => "inbox:delete:";
    public InboxDeleteCommand(IInboxRepository inbox, IUserGetter userGetter, CallbackQueryHandlersFactory factory) { _inbox = inbox; _userGetter = userGetter; _factory = factory; }
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string[] parts = update.CallbackQuery!.Data!.Split(':');
        long id = long.Parse(parts[2]);
        int page = parts.Length >= 4 && int.TryParse(parts[3], out int p) ? p : 1;
        await _inbox.DeleteAsync(id).ConfigureAwait(false);
        // go back to list
        update.CallbackQuery!.Data = $"inbox:list:{page}";
        await _factory.ExecuteAsync(update, botClient, ct).ConfigureAwait(false);
    }
}

