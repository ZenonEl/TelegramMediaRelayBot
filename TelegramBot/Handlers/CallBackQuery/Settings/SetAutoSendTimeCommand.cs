// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SetAutoSendTimeCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_auto_send_video_time_to:";

    private readonly IUserMenuService _userMenuService;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public SetAutoSendTimeCommand(IUserMenuService userMenuService, Config.Services.IResourceService resourceService, ITelegramInteractionService interactionService)
    {
        _userMenuService = userMenuService;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var callbackQueryData = update.CallbackQuery!.Data!.Split(':')[1];
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        // Заменяем static вызов на вызов сервиса
        var result = await _userMenuService.SetAutoSendVideoTimeToUser(chatId, callbackQueryData);

        var message = result
            ? _resourceService.GetResourceString("AutoSendTimeChangedMessage") + callbackQueryData
            : _resourceService.GetResourceString("AutoSendTimeNotChangedMessage");

        await _interactionService.ReplyToUpdate(botClient, update,
            KeyboardUtils.GetReturnButtonMarkup("user_set_auto_send_video_time"), ct, message);
    }
}
