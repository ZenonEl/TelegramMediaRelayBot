// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class ShowHelpCommand : IBotCallbackQueryHandlers
{
    private readonly Config.Services.IResourceService _resourceService;
    public string Name => "show_help";

    public ShowHelpCommand(Config.Services.IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var helpText = _resourceService.GetResourceString("HelpText");
        return botClient.EditMessageText(
            chatId: update.CallbackQuery!.Message!.Chat.Id,
            messageId: update.CallbackQuery!.Message!.MessageId,
            text: helpText,
            replyMarkup: KeyboardUtils.GetReturnButtonMarkup("main_menu"),
            cancellationToken: ct,
            parseMode: ParseMode.Html);
    }
}