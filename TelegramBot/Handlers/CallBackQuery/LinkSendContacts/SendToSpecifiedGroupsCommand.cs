using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToSpecifiedGroupsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IGroupMenuService _groupMenuService;
    private readonly DownloadSessionManager _sessionManager;

    public string Name => "send_to_specified_groups:";

    public SendToSpecifiedGroupsCommand(
        IUserStateManager stateManager, 
        IGroupMenuService groupMenuService,
        DownloadSessionManager sessionManager)
    {
        _stateManager = stateManager;
        _groupMenuService = groupMenuService;
        _sessionManager = sessionManager;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        CallbackQuery callbackQuery = update.CallbackQuery!;
        int messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        long chatId = callbackQuery.Message!.Chat.Id;

        _sessionManager.CancelDefaultAction(messageId);
        _sessionManager.MarkAsProcessing(messageId);

        UserStateData newState = new UserStateData
        {
            StateName = "SelectTargets",
            Step = 0,
            Data = new()
            {
                { "TargetType", "Groups" },
                { "SessionMessageId", messageId } 
            }
        };
        _stateManager.Set(chatId, newState);

        await _groupMenuService.ShowAvailableGroups(botClient, update);
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}