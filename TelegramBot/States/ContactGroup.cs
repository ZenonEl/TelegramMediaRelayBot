// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.


using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class EditContactGroupStateHandler : IStateHandler
{
    private readonly IContactGroupRepository _contactGroupRepository;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IContactGetter _contactGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "EditContactGroup";

    public EditContactGroupStateHandler(
        IContactGroupRepository contactGroupRepository,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        IContactGetter contactGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _contactGroupRepository = contactGroupRepository;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _contactGetter = contactGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update))
        {
            return StateResult.Complete();
        }

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Выбор действия (добавить/удалить участника)
            // ========================================================================
            case 0:
                if (update.CallbackQuery?.Data == null) return StateResult.Ignore();
                
                var callbackAction = update.CallbackQuery.Data;
                var userId = _userGetter.GetUserIDbyTelegramID(chatId);
                var groupId = int.Parse(callbackAction.Split(':')[1]);

                if (callbackAction.StartsWith("user_add_contact_to_group:"))
                {
                    stateData.Data["GroupId"] = groupId;
                    stateData.Data["Action"] = "Add";

                    var tgIds = await _contactGetter.GetAllContactUserTGIds(userId);
                    var infos = tgIds.Select(tg => {
                        int cid = _userGetter.GetUserIDbyTelegramID(tg);
                        string uname = _userGetter.GetUserNameByTelegramID(tg);
                        return string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, "");
                    }).ToList();
                    
                    var header = _resourceService.GetResourceString("YourContacts");
                    var prompt = _resourceService.GetResourceString("InputContactIDsText");
                    var body = infos.Any() ? string.Join("\n", infos) : _resourceService.GetResourceString("NoUsersFound");
                    
                    await botClient.SendMessage(chatId, $"{header}\n{body}\n\n{prompt}", cancellationToken: cancellationToken);
                    stateData.Step = 1;
                    return StateResult.Continue();
                }
                else if (callbackAction.StartsWith("user_remove_contact_from_group:"))
                {
                    stateData.Data["GroupId"] = groupId;
                    stateData.Data["Action"] = "Remove";

                    var members = await _groupGetter.GetAllUsersIdsInGroup(groupId);
                    var infos = members.Select(cid => {
                        string uname = _userGetter.GetUserNameByID(cid);
                        return string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, "");
                    }).ToList();

                    var header = _resourceService.GetResourceString("AllContactsText");
                    var body = infos.Any() ? string.Join("\n", infos) : _resourceService.GetResourceString("NoUsersFound");
                    var prompt = _resourceService.GetResourceString("InputContactIDsText");
                    
                    await botClient.SendMessage(chatId, $"{header}\n{body}\n\n{prompt}", cancellationToken: cancellationToken);
                    stateData.Step = 1;
                    return StateResult.Continue();
                }
                
                return StateResult.Ignore(); // Не наше действие

            // ========================================================================
            // ШАГ 1: Получение ID контактов и запрос подтверждения
            // ========================================================================
            case 1:
                if (update.Message?.Text == null) return StateResult.Ignore();
                if (!stateData.Data.TryGetValue("GroupId", out var groupIdObj) || !stateData.Data.TryGetValue("Action", out var actionObj))
                    return StateResult.Complete();

                groupId = (int)groupIdObj;
                var action = (string)actionObj;
                
                List<int> contactIDs;
                try
                {
                    contactIDs = update.Message.Text.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                }
                catch
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken);
                    return StateResult.Continue(); // Остаемся на том же шаге
                }

                if (!contactIDs.Any())
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }
                
                stateData.Data["ContactIDs"] = contactIDs;

                var confirmInfos = contactIDs.Select(cid => {
                    var uname = _userGetter.GetUserNameByID(cid);
                    return string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, "");
                }).ToList();

                if (action == "Add")
                {
                    var confirmHeader = _resourceService.GetResourceString("ConfirmAddContactsToGroupText");
                    await botClient.SendMessage(chatId, $"{confirmHeader}\n\n{string.Join("\n", confirmInfos)}",
                        replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_add"), cancellationToken: cancellationToken);
                }
                else // Remove
                {
                    var confirmHeader = _resourceService.GetResourceString("ConfirmDeleteContactsFromGroupText");
                    await botClient.SendMessage(chatId, $"{confirmHeader}\n\n{string.Join("\n", confirmInfos)}",
                        replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_remove"), cancellationToken: cancellationToken);
                }

                stateData.Step = 2;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 2: Финальное подтверждение и выполнение
            // ========================================================================
            case 2:
                if (update.CallbackQuery?.Data == null) return StateResult.Ignore();

                var confirmAction = update.CallbackQuery.Data;
                if (confirmAction == "decline")
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                    return StateResult.Complete();
                }

                if (!stateData.Data.TryGetValue("GroupId", out groupIdObj) || !stateData.Data.TryGetValue("ContactIDs", out var contactIdsObj))
                    return StateResult.Complete();
                
                groupId = (int)groupIdObj;
                var finalContactIds = (List<int>)contactIdsObj;
                var currentUserId = _userGetter.GetUserIDbyTelegramID(chatId);
                var success = true;

                if (confirmAction == "accept_add")
                {
                    foreach (var cid in finalContactIds)
                    {
                        if (!_contactGroupRepository.AddContactToGroup(currentUserId, cid, groupId)) success = false;
                    }
                }
                else if (confirmAction == "accept_remove")
                {
                    foreach (var cid in finalContactIds)
                    {
                        if (!_contactGroupRepository.RemoveContactFromGroup(currentUserId, cid, groupId)) success = false;
                    }
                }
                else
                {
                    return StateResult.Ignore();
                }

                var resultText = success ? _resourceService.GetResourceString("SuccessActionResult") : _resourceService.GetResourceString("ErrorActionResult");
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, resultText);
                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}