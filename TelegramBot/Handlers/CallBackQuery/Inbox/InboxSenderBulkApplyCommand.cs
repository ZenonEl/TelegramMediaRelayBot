using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class InboxSenderBulkApplyCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IInboxRepository _inbox;
    private readonly IResourceService _resourceService;
    public string Name => "inbox:sender:";
    public InboxSenderBulkApplyCommand(IUserGetter userGetter, IInboxRepository inbox, IResourceService resourceService) { _userGetter = userGetter; _inbox = inbox; _resourceService = resourceService; }
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
        await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, _resourceService.GetResourceString("SuccessActionResult"), cancellationToken: ct).ConfigureAwait(false);
        await new InboxSendersCommand(_userGetter, _inbox, _resourceService).ExecuteAsync(update, botClient, ct);
    }
}

