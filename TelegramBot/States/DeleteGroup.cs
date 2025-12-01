// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class UserDeleteGroupCommand : IBotCallbackQueryHandlers
{
    private readonly IGroupGetter _groupGetter;
    private readonly ITelegramInteractionService _interactionService;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly IUserGetter _userGetter;

    public string Name => "user_delete_group";

    public UserDeleteGroupCommand(
        IGroupGetter groupGetter,
        ITelegramInteractionService interactionService,
        Config.Services.IResourceService resourceService,
        IUserGetter userGetter)
    {
        _groupGetter = groupGetter;
        _interactionService = interactionService;
        _resourceService = resourceService;
        _userGetter = userGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = _interactionService.GetChatId(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        // 1. Получаем список групп
        // Используем тот же метод, что и для редактирования
        IEnumerable<int> groupsId = await _groupGetter.GetGroupIDsByUserId(userId);

        if (!groupsId.Any())
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, "У вас нет групп для удаления", cancellationToken: ct);
            return;
        }

        // 2. Генерируем кнопки
        List<InlineKeyboardButton[]> buttons = new List<InlineKeyboardButton[]>();
        foreach (int groupId in groupsId)
        {
            buttons.Add(new[]
            {
                // Клик по группе вызывает команду подтверждения удаления
                InlineKeyboardButton.WithCallbackData(
                    text: $"🗑 {await _groupGetter.GetGroupNameById(groupId)}",
                    callbackData: $"delete_group_select:{groupId}"
                )
            });
        }

        // Кнопка "Назад" к списку действий с группами
        buttons.Add(new[] { KeyboardUtils.GetReturnButton("show_groups") });

        // TODO: Move "Group.Delete.SelectPrompt"
        string text = "🗑 <b>Выберите группу для удаления:</b>\n<i>Внимание: это действие необратимо.</i>";

        await _interactionService.ReplyToUpdate(
            botClient,
            update,
            new InlineKeyboardMarkup(buttons),
            ct,
            text
        );

        await botClient.AnswerCallbackQuery(update.CallbackQuery!.Id, cancellationToken: ct);
    }
}
