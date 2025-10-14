using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.Database;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IUserMenuService _userMenuService;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public SetVideoSendUsersCommand(
        IUserStateManager stateManager,
        IUserMenuService userMenuService,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _stateManager = stateManager;
        _userMenuService = userMenuService;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public string Name => "user_set_video_send_users:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var parts = update.CallbackQuery!.Data!.Split(':');
        var action = parts[1];
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        // --- ЛОГИКА ЗАПУСКА СОСТОЯНИЙ ---
        if (action == UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS || action == UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS)
        {
            var isGroup = action == UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS;
            
            // Запускаем новый, чистый StateHandler
            var newState = new UserStateData
            {
                StateName = isGroup ? "SetDistributionGroups" : "SetDistributionUsers",
                Step = 0
            };
            _stateManager.Set(chatId, newState);

            // Отправляем первое сообщение (логика показа списка контактов/групп теперь внутри StateHandler)
            var prompt = _resourceService.GetResourceString("EnterContactIdsPrompt");
            await botClient.SendMessage(chatId, prompt, cancellationToken: cancellationToken);
            
            // Также обновляем действие по умолчанию, если нужно
            await _userMenuService.SetDefaultActionToUser(chatId, action);
            return;
        }

        // --- ЛОГИКА ПРЯМОГО ДЕЙСТВИЯ ---
        var result = await _userMenuService.SetDefaultActionToUser(chatId, action);
        
        var message = result 
            ? _resourceService.GetResourceString("DefaultActionChangedMessage")
            : _resourceService.GetResourceString("DefaultActionNotChangedMessage");

        await _interactionService.ReplyToUpdate(botClient, update, 
            KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"), cancellationToken, message);
    }
}