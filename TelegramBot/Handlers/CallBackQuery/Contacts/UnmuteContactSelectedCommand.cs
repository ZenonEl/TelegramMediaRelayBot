// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class UnmuteContactSelectedCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRepository _contactSetter;
    private readonly IUserGetter _userGetter;
    private readonly IUserStateManager _stateManager;
    private readonly ITelegramInteractionService _interactionService;
    private readonly Config.Services.IResourceService _resourceService;

    public string Name => "unmute_contact_select:";

    public UnmuteContactSelectedCommand(
        IContactRepository contactSetter,
        IUserGetter userGetter,
        IUserStateManager stateManager,
        ITelegramInteractionService interactionService,
        Config.Services.IResourceService resourceService)
    {
        _contactSetter = contactSetter;
        _userGetter = userGetter;
        _stateManager = stateManager;
        _interactionService = interactionService;
        _resourceService = resourceService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        CallbackQuery callback = update.CallbackQuery!;
        long chatId = _interactionService.GetChatId(update);
        int myUserId = _userGetter.GetUserIDbyTelegramID(chatId);

        if (!int.TryParse(callback.Data!.Split(':')[1], out int contactId))
        {
            await botClient.AnswerCallbackQuery(callback.Id, "Invalid ID", cancellationToken: ct);
            return;
        }

        await _contactSetter.DeactivateMutedContactAsync(myUserId, contactId);

        string contactName = _userGetter.GetUserNameByID(contactId) ?? "Unknown";

        // TODO Move: "Unmute.Success"
        string text = $"🔊 Пользователь <b>{contactName}</b> размучен.\nТеперь вы будете получать от него медиа.";

        await _interactionService.ReplyToUpdate(
            botClient,
            update,
            KeyboardUtils.SendInlineKeyboardMenu(),
            ct,
            text
        );

        _stateManager.Set(chatId, new UserStateData { StateName = "None" });

        await botClient.AnswerCallbackQuery(callback.Id, "Размучен", cancellationToken: ct);
    }
}
