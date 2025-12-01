// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class ManageContactsStateHandler : IStateHandler
{
    private readonly IUiResourceService _uiResources;
    private readonly ISettingsResourceService _settingsResources;
    private readonly IErrorsResourceService _errorsResources;
    private readonly IContactRemover _contactRemover;
    private readonly IContactGetter _contactGetter;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "ManageContacts";

    public ManageContactsStateHandler(
        IUiResourceService uiResources,
        ISettingsResourceService settingsResources,
        IErrorsResourceService errorsResources,
        IContactRemover contactRemover,
        IContactGetter contactGetter,
        IUserRepository userRepository,
        IUserGetter userGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _uiResources = uiResources;
        _settingsResources = settingsResources;
        _errorsResources = errorsResources;
        _contactRemover = contactRemover;
        _contactGetter = contactGetter;
        _userRepository = userRepository;
        _userGetter = userGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        long chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update))
        {
            return StateResult.Complete();
        }

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Ожидание списка ID от пользователя
            // ========================================================================
            case 0:
                string? messageText = update.Message?.Text;
                if (string.IsNullOrEmpty(messageText))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("State.ContactLinks.InvalidInput"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                int actingUserId = _userGetter.GetUserIDbyTelegramID(chatId);
                List<int> inputIds;
                try
                {
                    inputIds = messageText.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                }
                catch
                {
                    await botClient.SendMessage(chatId, _errorsResources.GetString("Error.Input.Generic"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                if (inputIds.Count == 0)
                {
                    // Логика показа списка контактов, если ввод некорректен
                    List<long> tgIds = await _contactGetter.GetAllContactUserTGIds(actingUserId);
                    List<string> lines = tgIds.Select(tg =>
                    {
                        int cid = _userGetter.GetUserIDbyTelegramID(tg);
                        string uname = _userGetter.GetUserNameByTelegramID(tg);
                        return string.Format(_uiResources.GetString("UI.Format.ContactInfo"), cid, uname, "");
                    }).ToList();

                    string header = _uiResources.GetString("UI.Header.YourContacts");
                    string body = lines.Any() ? string.Join("\n", lines) : _errorsResources.GetString("Error.Input.NoUsersFound");
                    string prompt = _resourceService.GetResourceString("State.ContactLinks.NoValidIds");
                    await botClient.SendMessage(chatId, $"{header}\n{body}\n\n{prompt}", cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                List<long> allowedTgIds = await _contactGetter.GetAllContactUserTGIds(actingUserId);
                List<int> validTargetIds = inputIds.Where(id => allowedTgIds.Contains(_userGetter.GetTelegramIDbyUserID(id))).ToList();

                if (validTargetIds.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("State.ContactLinks.NoValidIdsForAccount"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                stateData.Data["TargetIds"] = validTargetIds;

                string idsList = string.Join(", ", validTargetIds);
                string message = string.Format(_resourceService.GetResourceString("State.ContactLinks.ConfirmList"), idsList);

                await botClient.SendMessage(chatId, message, replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);

                stateData.Step = 1;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения
            // ========================================================================
            case 1:
                if (update.CallbackQuery?.Data == null) return StateResult.Ignore();
                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);

                if (update.CallbackQuery.Data != "accept")
                {
                    // Если пользователь нажал "decline", просто завершаем
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                    return StateResult.Complete();
                }

                // Пользователь нажал "accept", выполняем действие
                if (!stateData.Data.TryGetValue("IsDelete", out object? isDeleteObj) ||
                    !stateData.Data.TryGetValue("TargetIds", out object? targetIdsObj))
                {
                    return StateResult.Complete();
                }

                bool isDelete = (bool)isDeleteObj;
                List<int> targetIds = (List<int>)targetIdsObj;
                int currentUserId = _userGetter.GetUserIDbyTelegramID(chatId);

                bool actionStatus;
                if (isDelete)
                {
                    actionStatus = await _contactRemover.RemoveUsersFromContacts(currentUserId, targetIds);
                }
                else
                {
                    actionStatus = await _contactRemover.RemoveAllContactsExcept(currentUserId, targetIds);
                }

                string statusMessage = actionStatus ? _uiResources.GetString("UI.Success") : _errorsResources.GetString("Error.ActionFailed");

                _userRepository.ReCreateUserSelfLink(currentUserId);

                await _interactionService.ReplyToUpdate(botClient, update, UsersPrivacyMenuKB.GetUpdateSelfLinkKeyboardMarkup(),
                    cancellationToken, _settingsResources.GetString("Settings.Link.Menu.Title") + "\n\n" + statusMessage);

                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}
