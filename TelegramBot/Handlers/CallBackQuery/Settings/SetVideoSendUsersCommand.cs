// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    private readonly IStatesResourceService _statesResources;
    private readonly IUserStateManager _stateManager;
    private readonly IUserMenuService _userMenuService;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public SetVideoSendUsersCommand(
        IStatesResourceService statesResources,
        IUserStateManager stateManager,
        IUserMenuService userMenuService,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _statesResources = statesResources;
        _stateManager = stateManager;
        _userMenuService = userMenuService;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public string Name => "user_set_video_send_users:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string[] parts = update.CallbackQuery!.Data!.Split(':');
        string action = parts[1];
        long chatId = update.CallbackQuery!.Message!.Chat.Id;

        // --- ЛОГИКА ЗАПУСКА СОСТОЯНИЙ ---
        if (action == UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS || action == UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS)
        {
            bool isGroup = action == UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS;

            // Запускаем новый, чистый StateHandler
            UserStateData newState = new UserStateData
            {
                StateName = isGroup ? "SetDistributionGroups" : "SetDistributionUsers",
                Step = 0
            };
            _stateManager.Set(chatId, newState);

            // Отправляем первое сообщение (логика показа списка контактов/групп теперь внутри StateHandler)
            string prompt = _statesResources.GetString("State.UpdateLink.Prompt.EnterIds");
            await botClient.SendMessage(chatId, prompt, cancellationToken: cancellationToken);

            // Также обновляем действие по умолчанию, если нужно
            await _userMenuService.SetDefaultActionToUser(chatId, action);
            return;
        }

        // --- ЛОГИКА ПРЯМОГО ДЕЙСТВИЯ ---
        bool result = await _userMenuService.SetDefaultActionToUser(chatId, action);

        string message = result
            ? _statesResources.GetString("State.DefaultAction.ActionChanged")
            : _statesResources.GetString("State.DefaultAction.ActionNotChanged");

        await _interactionService.ReplyToUpdate(botClient, update,
            KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"), cancellationToken, message);
    }
}
