using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToSpecifiedGroupsCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IGroupMenuService _groupMenuService; // Используем сервис для групп

    public string Name => "send_to_specified_groups:";

    public SendToSpecifiedGroupsCommand(IUserStateManager stateManager, IGroupMenuService groupMenuService)
    {
        _stateManager = stateManager;
        _groupMenuService = groupMenuService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var callbackQuery = update.CallbackQuery!;
        var messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        var chatId = callbackQuery.Message!.Chat.Id;

        // Запускаем тот же StateHandler, но с другим TargetType
        var newState = new UserStateData
        {
            StateName = "SelectTargets",
            Step = 0,
            Data = new()
            {
                { "TargetType", "Groups" }, // <--- Ключевое отличие
                { "SessionMessageId", messageId } 
            }
        };
        _stateManager.Set(chatId, newState);

        // Показываем пользователю его группы, чтобы он мог выбрать
        await _groupMenuService.ShowAvailableGroups(botClient, update); // Нужен такой метод
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}