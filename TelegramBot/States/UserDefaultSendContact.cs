// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class SetDistributionUsersStateHandler : IStateHandler
{
    private readonly IContactGetter _contactGetter;
    private readonly IDefaultAction _defaultAction;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "SetDistributionUsers";

    public SetDistributionUsersStateHandler(
        IContactGetter contactGetter,
        IDefaultAction defaultAction,
        IDefaultActionGetter defaultActionGetter,
        IUserGetter userGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _contactGetter = contactGetter;
        _defaultAction = defaultAction;
        _defaultActionGetter = defaultActionGetter;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        long chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update)) return StateResult.Complete();

        int actingUserId = _userGetter.GetUserIDbyTelegramID(chatId);

        switch (stateData.Step)
        {
            // ШАГ 0: Ожидание списка ID контактов
            case 0:
                string? messageText = update.Message?.Text;
                if (string.IsNullOrEmpty(messageText))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                List<int> inputIds;
                try { inputIds = messageText.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList(); }
                catch { /* ... обработка ошибки парсинга ... */ return StateResult.Continue(); }

                if (inputIds.Count == 0)
                {
                    List<long> tgIds = await _contactGetter.GetAllContactUserTGIds(actingUserId);
                    List<string> infos = tgIds.Select(tg =>
                    {
                        int cid = _userGetter.GetUserIDbyTelegramID(tg);
                        string uname = _userGetter.GetUserNameByTelegramID(tg);
                        return string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, "");
                    }).ToList();
                    string header = _resourceService.GetResourceString("YourContacts");
                    string body = infos.Any() ? string.Join("\n", infos) : _resourceService.GetResourceString("NoUsersFound");
                    await botClient.SendMessage(chatId, $"{header}\n{body}\n\n{_resourceService.GetResourceString("PleaseEnterContactIDs")}", cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                List<long> allowedTgIds = await _contactGetter.GetAllContactUserTGIds(actingUserId);
                List<int> validTargetIds = inputIds.Where(id => allowedTgIds.Contains(_userGetter.GetTelegramIDbyUserID(id))).ToList();

                if (validTargetIds.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("NoUsersFound"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                stateData.Data["TargetIds"] = validTargetIds;
                string idsList = string.Join(", ", validTargetIds);
                string message = string.Format(_resourceService.GetResourceString("ProcessIDsList"), idsList);

                await botClient.SendMessage(chatId, message, replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                stateData.Step = 1;
                return StateResult.Continue();

            // ШАГ 1: Ожидание подтверждения
            case 1:
                if (update.CallbackQuery?.Data != "accept")
                {
                    await _interactionService.ReplyToUpdate(botClient, update, UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
                        cancellationToken, _resourceService.GetResourceString("UsersVideoSentUsersMenuText"));
                    return StateResult.Complete();
                }

                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);

                List<int> targetIds = (List<int>)stateData.Data["TargetIds"];
                int actionId = _defaultActionGetter.GetDefaultActionId(actingUserId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);

                await _defaultAction.RemoveAllDefaultUsersActionTargets(actingUserId, TargetTypes.USER, actionId);
                foreach (int id in targetIds)
                {
                    await _defaultAction.AddDefaultUsersActionTargets(actingUserId, actionId, TargetTypes.USER, id);
                }

                await _interactionService.ReplyToUpdate(botClient, update, UsersDefaultActionsMenuKB.GetUsersVideoSentUsersKeyboardMarkup(),
                    cancellationToken, string.Format(_resourceService.GetResourceString("SuccessMessageProcessIDsList"), targetIds.Count));

                return StateResult.Complete();
        }
        return StateResult.Ignore();
    }
}
