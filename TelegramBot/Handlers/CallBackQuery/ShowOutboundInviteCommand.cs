// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class ShowOutboundInviteCommand : IBotCallbackQueryHandlers
{
    private readonly ICallbackQueryMenuService _menuService;
    public string Name => "user_show_outbound_invite:";

    public ShowOutboundInviteCommand(ICallbackQueryMenuService menuService)
    {
        _menuService = menuService;
    }

    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _menuService.ShowOutboundInvite(botClient, update);
    }
}