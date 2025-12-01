// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.TelegramBot.States;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class SendToSpecifiedUsersCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IContactMenuService _contactMenuService;
    private readonly DownloadSessionManager _sessionManager;

    public string Name => "send_to_specified_users:";

    public SendToSpecifiedUsersCommand(
        IUserStateManager stateManager,
        IContactMenuService contactMenuService,
        DownloadSessionManager sessionManager)
    {
        _stateManager = stateManager;
        _contactMenuService = contactMenuService;
        _sessionManager = sessionManager;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        CallbackQuery callbackQuery = update.CallbackQuery!;
        int messageId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        long chatId = callbackQuery.Message!.Chat.Id;

        _sessionManager.CancelDefaultAction(messageId);
        _sessionManager.MarkAsProcessing(messageId);

        var sentMessage = await _contactMenuService.ShowAvailableContacts(botClient, update);

        var stateDataDict = new Dictionary<string, object>
        {
            { "TargetType", "Users" },
            { "SessionMessageId", messageId }
        };

        if (sentMessage != null)
        {
            stateDataDict.Add("ContactListId", sentMessage.MessageId);
        } //TODO сделать тоже самое и для групп

        UserStateData newState = new UserStateData
        {
            StateName = "SelectTargets",
            Step = 0,
            Data = stateDataDict
        };
        _stateManager.Set(chatId, newState);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
    }
}
