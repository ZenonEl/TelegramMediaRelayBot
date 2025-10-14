using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.TelegramBot.Handlers;

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

