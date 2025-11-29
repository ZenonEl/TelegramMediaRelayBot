// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IStateBreakService
{
    Task<bool> HandleStateBreak(ITelegramBotClient botClient, Update update);
    Task AlertAndShowMenu(ITelegramBotClient botClient, Update update, string alertText);
}

public class StateBreakService : IStateBreakService
{
    private readonly IUserStateManager _stateManager;
    private readonly ITelegramInteractionService _interactionService;
    // В будущем мы заменим KeyboardUtils на IKeyboardService
    // private readonly IKeyboardService _keyboardService; 

    public StateBreakService(IUserStateManager stateManager, ITelegramInteractionService interactionService)
    {
        _stateManager = stateManager;
        _interactionService = interactionService;
    }

    public async Task<bool> HandleStateBreak(ITelegramBotClient botClient, Update update)
    {
        const string command = "/start";
        const string callbackData = "main_menu";

        if ((update.Message?.Text == command) || (update.CallbackQuery?.Data == callbackData))
        {
            var chatId = GetChatId(update);
            _stateManager.Remove(chatId);
            await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), CancellationToken.None);
            return true;
        }
        return false;
    }

    public async Task AlertAndShowMenu(ITelegramBotClient botClient, Update update, string alertText)
    {
        var chatId = GetChatId(update);
        _stateManager.Remove(chatId);
        await botClient.SendMessage(chatId, alertText, cancellationToken: CancellationToken.None);
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), CancellationToken.None);
    }
    
    // Внутренний хелпер, чтобы не зависеть от ITelegramInteractionService внутри этого же сервиса
    private long GetChatId(Update update) => update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? 0;
}