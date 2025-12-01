// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.CallBackQuery.Contacts;

public class UserEditGroupCommand : IBotCallbackQueryHandlers
{
    private readonly IContactMenuService _groupMenuService;

    public string Name => "user_edit_group";

    public UserEditGroupCommand(IContactMenuService groupMenuService)
    {
        _groupMenuService = groupMenuService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _groupMenuService.StartEditContactGroupFlow(botClient, update);
    }
}
