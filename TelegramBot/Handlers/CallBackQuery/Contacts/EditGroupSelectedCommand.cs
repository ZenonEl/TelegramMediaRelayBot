// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.Database.Interfaces; // Для IGroupGetter

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class EditGroupSelectedCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly ITelegramInteractionService _interactionService;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly IGroupGetter _groupGetter; // Добавили геттер

    public string Name => "edit_group_selected:";

    public EditGroupSelectedCommand(
        IUserStateManager stateManager,
        ITelegramInteractionService interactionService,
        Config.Services.IResourceService resourceService,
        IGroupGetter groupGetter)
    {
        _stateManager = stateManager;
        _interactionService = interactionService;
        _resourceService = resourceService;
        _groupGetter = groupGetter;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        CallbackQuery callback = update.CallbackQuery!;
        long chatId = callback.Message!.Chat.Id;
        
        if (!int.TryParse(callback.Data!.Split(':')[1], out int groupId))
        {
            await botClient.AnswerCallbackQuery(callback.Id, "Invalid Group ID", cancellationToken: ct);
            return;
        }

        // Получаем информацию о группе, чтобы узнать её имя и IsDefault
        // TODO: Убедись, что метод GetGroupById существует в репозитории. Если нет - добавь.
        // Если его нет, используй GetGroupsByUserId и найди нужную в списке.
        string groupName = await _groupGetter.GetGroupNameById(groupId); 
        
        if (groupName == null)
        {
            await botClient.AnswerCallbackQuery(callback.Id, "Group not found", cancellationToken: ct);
            return;
        }

        UserStateData stateData = new UserStateData
        {
            StateName = "EditContactGroup",
            Step = 0,
            Data = new Dictionary<string, object>
            {
                { "GroupId", groupId },
                { "GroupName", groupName } // Сохраняем имя для заголовка
            }
        };
        _stateManager.Set(chatId, stateData);

        // Формируем статус для кнопки "По умолчанию"
        // TODO: Move "Group.IsDefault.On" / "Off"
        string defaultStatusIcon = await _groupGetter.GetIsDefaultGroup(groupId) ? "✅" : "❌";
        string defaultBtnText = $"📢 Рассылка по умолчанию: {defaultStatusIcon}";

        InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] 
            {
                // TODO: Move "Group.Action.Rename"
                InlineKeyboardButton.WithCallbackData("✏️ Переименовать", "group_action:rename"),
                // TODO: Move "Group.Action.ToggleDefault"
                InlineKeyboardButton.WithCallbackData(defaultBtnText, "group_action:toggle_default")
            },
            new[] 
            {
                // TODO: Move "Group.AddMember"
                InlineKeyboardButton.WithCallbackData("➕ Добавить участника", "group_action:add"),
                // TODO: Move "Group.RemoveMember"
                InlineKeyboardButton.WithCallbackData("➖ Удалить участника", "group_action:remove")
            },
            new[] 
            { 
                KeyboardUtils.GetReturnButton("show_groups")
            }
        });

        // TODO: Move "Group.EditMenu.Title"
        string text = $"📂 <b>Редактирование группы: {groupName}</b>\n\nЧто вы хотите сделать?";

        await _interactionService.ReplyToUpdate(
            botClient, 
            update, 
            keyboard, 
            ct, 
            text
        );

        await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
    }
}