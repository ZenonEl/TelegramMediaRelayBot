// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class MuteContactSelectedCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly ITelegramInteractionService _interactionService;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly IEnumerable<IStateHandler> _stateHandlers;

    public string Name => "mute_contact_select:";

    public MuteContactSelectedCommand(
        IUserStateManager stateManager,
        ITelegramInteractionService interactionService,
        Config.Services.IResourceService resourceService,
        IEnumerable<IStateHandler> stateHandlers)
    {
        _stateManager = stateManager;
        _interactionService = interactionService;
        _resourceService = resourceService;
        _stateHandlers = stateHandlers;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        CallbackQuery callback = update.CallbackQuery!;
        long chatId = _interactionService.GetChatId(update);

        // data format: "mute_contact_select:{contactId}"
        if (!int.TryParse(callback.Data!.Split(':')[1], out int contactId))
        {
            await botClient.AnswerCallbackQuery(callback.Id, "Invalid Contact ID", cancellationToken: ct);
            return;
        }

        UserStateData stateData = new UserStateData
        {
            StateName = "MuteUser",
            Step = 0, // Шаг 0 = Выбор времени
            Data = new Dictionary<string, object>
            {
                { "MutedContactId", contactId }
            }
        };
        _stateManager.Set(chatId, stateData);

        IStateHandler? handler = _stateHandlers.FirstOrDefault(h => h.Name == "MuteUser");
        if (handler != null)
        {
            await handler.Process(stateData, update, botClient, ct);
        }

        await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
    }
}
