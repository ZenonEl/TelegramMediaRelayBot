using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToSpecifiedUsersCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IContactMenuService _contactMenuService;
    private readonly DownloadSessionManager _sessionManager;

    public string Name => "send_to_specified_users:";

    public SendToSpecifiedUsersCommand(
        IUserStateManager stateManager, 
        IContactMenuService contactMenuService,
        DownloadSessionManager sessionManager)
    {
        _stateManager = stateManager;
        _contactMenuService = contactMenuService;
        _sessionManager = sessionManager;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        CallbackQuery callbackQuery = update.CallbackQuery!;
        int messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        long chatId = callbackQuery.Message!.Chat.Id;

        _sessionManager.CancelDefaultAction(messageId);

        UserStateData newState = new UserStateData
        {
            StateName = "SelectTargets",
            Step = 0,
            Data = new()
            {
                { "TargetType", "Users" },
                { "SessionMessageId", messageId } 
            }
        };
        _stateManager.Set(chatId, newState);
        
        await _contactMenuService.ShowAvailableContacts(botClient, update);
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}