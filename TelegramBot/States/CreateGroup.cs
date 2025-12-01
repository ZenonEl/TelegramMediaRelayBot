// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class CreateContactGroupStateHandler : IStateHandler
{
    private readonly IGroupSetter _groupSetter;
    private readonly IUserGetter _userGetter;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;
    private readonly Config.Services.IResourceService _resourceService;

    public string Name => "CreateContactGroup";

    public CreateContactGroupStateHandler(
        IGroupSetter groupSetter,
        IUserGetter userGetter,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker,
        Config.Services.IResourceService resourceService)
    {
        _groupSetter = groupSetter;
        _userGetter = userGetter;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
        _resourceService = resourceService;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        long chatId = _interactionService.GetChatId(update);

        // Логика отмены (кнопка)
        if (await _stateBreaker.HandleStateBreak(botClient, update))
        {
            return StateResult.Complete();
        }

        if (stateData.Step != 0) return StateResult.Ignore();

        // 1. Проверяем наличие текста
        string? groupName = update.Message?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(groupName) || groupName.Length > 100)
        {
            // TODO: Move "Group.Create.ErrorName"
            await botClient.SendMessage(chatId, "⚠️ Название группы не может быть пустым или слишком длинным (макс. 100 символов).", cancellationToken: cancellationToken);
            return StateResult.Continue();
        }

        // 2. Создание группы
        int ownerId = _userGetter.GetUserIDbyTelegramID(chatId);

        // TODO: Реализовать метод CreateGroup (он должен вернуть ID новой группы)
        bool newGroupId = await _groupSetter.SetNewGroup(ownerId, groupName, "");

        // 3. Финальное сообщение
        if (newGroupId)
        {
            // TODO: Move "Group.Create.Success"
            string successText = $"✅ Группа <b>{groupName}</b> (ID: {newGroupId}) успешно создана!";
            await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, successText);
        }
        else
        {
            // TODO: Move "Group.Create.Error"
            string errorText = "❌ Не удалось создать группу. Попробуйте другое имя.";
            await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, errorText);
        }

        return StateResult.Complete();
    }
}
