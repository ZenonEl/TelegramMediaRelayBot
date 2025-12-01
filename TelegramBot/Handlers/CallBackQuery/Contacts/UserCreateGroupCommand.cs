// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class UserCreateGroupCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly ITelegramInteractionService _interactionService;
    private readonly Config.Services.IResourceService _resourceService;

    public string Name => "user_create_group";

    public UserCreateGroupCommand(
        IUserStateManager stateManager,
        ITelegramInteractionService interactionService,
        Config.Services.IResourceService resourceService)
    {
        _stateManager = stateManager;
        _interactionService = interactionService;
        _resourceService = resourceService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;

        // 1. Устанавливаем состояние.
        _stateManager.Set(chatId, new UserStateData { StateName = "CreateContactGroup", Step = 0 });

        // 2. Отправляем запрос пользователю (с возможностью отмены)
        // TODO: Move "Group.Create.Prompt"
        string promptText = "📝 <b>Введите название новой группы:</b>\nНапример: <i>Рабочие чаты</i>";

        // Кнопка "Отмена" должна быть в replyMarkup, но для простоты UX оставим одну кнопку.
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup keyboard = KeyboardUtils.GetCancelKeyboardMarkup(callback: "main_menu"); // Используем ID 0, чтобы получить новую кнопку

        await _interactionService.ReplyToUpdate(
            botClient,
            update,
            keyboard,
            ct,
            promptText
        );

        await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
    }
}
