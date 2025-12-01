// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class WhosTheGeniusCommand(IResourceService recourceService, ITelegramInteractionService interactionService) : IBotCallbackQueryHandlers
{
    private readonly IResourceService _recourceService = recourceService;
    private readonly ITelegramInteractionService _interactionService = interactionService;
    public string Name => "whos_the_genius";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string text = _recourceService.GetResourceString("WhosTheGeniusText");
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), ct, text);
    }
}
