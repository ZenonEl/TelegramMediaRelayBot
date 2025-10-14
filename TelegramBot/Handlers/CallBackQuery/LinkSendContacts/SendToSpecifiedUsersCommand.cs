using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToSpecifiedUsersCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IContactMenuService _contactMenuService;
    public string Name => "send_to_specified_users:";

    public SendToSpecifiedUsersCommand(IUserStateManager stateManager, IContactMenuService contactMenuService)
    {
        _stateManager = stateManager;
        _contactMenuService = contactMenuService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var callbackQuery = update.CallbackQuery!;
        var messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        var chatId = callbackQuery.Message!.Chat.Id;

        // Запускаем наш StateHandler для выбора контактов
        var newState = new UserStateData
        {
            StateName = "SelectTargets",
            Step = 0,
            Data = new()
            {
                { "TargetType", "Users" },
                // Сохраняем ID сессии, чтобы вернуться к ней после выбора
                { "SessionMessageId", messageId } 
            }
        };
        _stateManager.Set(chatId, newState);

        // Показываем пользователю его контакты, чтобы он мог выбрать
        await _contactMenuService.ShowAvailableContacts(botClient, update); // Нужен такой метод в сервисе
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}