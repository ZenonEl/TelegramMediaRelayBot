// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class DeleteGroupSelectedCommand : IBotCallbackQueryHandlers
{
    private readonly IGroupGetter _groupGetter;
    private readonly IGroupSetter _groupSetter; // Вот он, наш Setter
    private readonly ITelegramInteractionService _interactionService;
    private readonly Config.Services.IResourceService _resourceService;

    // Эта команда будет ловить всё, что начинается с "delete_group_"
    // Благодаря нашей "умной" фабрике, она поймает и _select, и _confirm (если правильно зарегистрировать)
    // НО: лучше сделаем два отдельных префикса или один умный.
    // Давай используем один класс на два префикса, если фабрика это позволяет (нет, фабрика ищет по Name).
    // Поэтому сделаем Name общим префиксом, а внутри разберемся.
    public string Name => "delete_group_";

    public DeleteGroupSelectedCommand(
        IGroupGetter groupGetter,
        IGroupSetter groupSetter,
        ITelegramInteractionService interactionService,
        Config.Services.IResourceService resourceService)
    {
        _groupGetter = groupGetter;
        _groupSetter = groupSetter;
        _interactionService = interactionService;
        _resourceService = resourceService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var data = update.CallbackQuery!.Data!;
        // data formats:
        // 1. "delete_group_select:123"
        // 2. "delete_group_confirm:123"

        var parts = data.Split(':');
        string action = parts[0]; // "delete_group_select" или "delete_group_confirm"

        if (parts.Length < 2 || !int.TryParse(parts[1], out int groupId))
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "Error parsing ID", cancellationToken: ct);
            return;
        }

        // --- СЦЕНАРИЙ 1: ЗАПРОС ПОДТВЕРЖДЕНИЯ ---
        if (action == "delete_group_select")
        {
            var group = await _groupGetter.GetIsDefaultGroup(groupId);
            if (group == null)
            {
                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "Group not found", cancellationToken: ct);
                return;
            }

            // Рисуем кнопки "Да, удалить" и "Нет, назад"
            var confirmKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    // Кнопка подтверждения
                    InlineKeyboardButton.WithCallbackData("💥 Да, удалить", $"delete_group_confirm:{groupId}")
                },
                new[]
                {
                    // Кнопка отмены (возврат к списку удаления)
                    InlineKeyboardButton.WithCallbackData("🔙 Нет, отмена", "user_delete_group")
                }
            });

            // TODO: Move "Group.Delete.ConfirmPrompt"
            string text = $"❓ <b>Вы уверены, что хотите удалить группу «{await _groupGetter.GetGroupNameById(groupId)}»?</b>\n\nЭто удалит группу, но контакты останутся в вашем списке.";

            await _interactionService.ReplyToUpdate(botClient, update, confirmKeyboard, ct, text);
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
        }
        // --- СЦЕНАРИЙ 2: ВЫПОЛНЕНИЕ УДАЛЕНИЯ ---
        else if (action == "delete_group_confirm")
        {
            // Вызываем IGroupSetter
            // TODO: Проверь, как называется метод удаления в твоем IGroupSetter
            // Скорее всего: Task DeleteGroupAsync(int groupId);
            bool success = await _groupSetter.SetDeleteGroup(groupId);

            if (success)
            {
                // TODO: Move "Group.Delete.Success"
                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "✅ Группа удалена", cancellationToken: ct);

                // Возвращаем пользователя к списку групп (чтобы он видел, что удалилось)
                // Для этого просто вызываем логику списка
                // Но так как у нас нет прямой ссылки на команду списка, просто отправим сообщение с кнопкой меню

                await _interactionService.ReplyToUpdate(
                    botClient,
                    update,
                    KeyboardUtils.SendInlineKeyboardMenu(), // Или кнопка "Назад к группам"
                    ct,
                    "✅ <b>Группа успешно удалена.</b>"
                );
            }
            else
            {
                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "❌ Ошибка при удалении", cancellationToken: ct);
            }
        }
    }
}
