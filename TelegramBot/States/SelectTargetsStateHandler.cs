using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class SelectTargetsStateHandler : IStateHandler
{
    private readonly IUserGetter _userGetter;
    private readonly IContactGetter _contactGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;
    private readonly DownloadSessionManager _sessionManager;
    private readonly IMediaProcessingFlow _mediaFlow;

    public string Name => "SelectTargets";

    public SelectTargetsStateHandler(
        IUserGetter userGetter,
        IContactGetter contactGetter,
        IGroupGetter groupGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker,
        DownloadSessionManager sessionManager,
        IMediaProcessingFlow mediaFlow)
    {
        _userGetter = userGetter;
        _contactGetter = contactGetter;
        _groupGetter = groupGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
        _sessionManager = sessionManager;
        _mediaFlow = mediaFlow;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update)) return StateResult.Complete();

        // --- НОВАЯ ЛОГИКА ДЛЯ КНОПКИ "НАЗАД" ---
        if (update.CallbackQuery?.Data.Split(':')[0] == "cancel_download:")
        {
            int sessionId = int.Parse(update.CallbackQuery.Data.Split(':')[1]);
            await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetVideoDistributionKeyboardMarkup(sessionId), cancellationToken, _resourceService.GetResourceString("VideoRecipientsButtonText"));
            return StateResult.Complete();
        }

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Ожидание списка ID
            // ========================================================================
            case 0:
                string messageText = update.Message?.Text;
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.Split(':')[0] != "cancel_download:")
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetVideoDistributionKeyboardMarkup(update.CallbackQuery.Message.Id), cancellationToken, _resourceService.GetResourceString("VideoRecipientsButtonText"));
                    return StateResult.Complete();
                }
                else if (string.IsNullOrEmpty(messageText))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InvalidInputValues"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                // ... (вся твоя логика парсинга и валидации inputIds остается без изменений) ...
                List<int> validTargetIds = await ParseAndValidateIds(chatId, messageText, stateData);
                if (validTargetIds == null) return StateResult.Continue(); // Ошибка парсинга/валидации

                stateData.Data["SelectedTargetIds"] = validTargetIds;

                // Формируем сообщение для подтверждения
                var idsList = string.Join(", ", validTargetIds);
                var message = string.Format(_resourceService.GetResourceString("ProcessIDsList"), idsList);

                // Отправляем новое сообщение с кнопками "Да" / "Назад"
                await botClient.SendMessage(chatId, message,
                    replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept", "cancel_download:" + stateData.Data["SessionMessageId"]),
                    cancellationToken: cancellationToken);

                stateData.Step = 1; // Переходим на шаг подтверждения
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Ожидание подтверждения (Да/Назад)
            // ========================================================================
            case 1:
                if (update.CallbackQuery?.Data != "accept")
                {
                    // Если нажали не "Да", то мы считаем это отменой (логика "Назад" обработана выше)
                    int sessionId = int.Parse(update.CallbackQuery.Data.Split(':')[1]);
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetVideoDistributionKeyboardMarkup(sessionId), cancellationToken, _resourceService.GetResourceString("VideoRecipientsButtonText"));
                    return StateResult.Complete();
                }

                try
                {
                    await botClient.DeleteMessage(chatId, update.CallbackQuery.Message!.MessageId, cancellationToken: cancellationToken);
                }
                catch { }

                if (!stateData.Data.TryGetValue("SessionMessageId", out var sessionMsgIdObj) ||
                    !stateData.Data.TryGetValue("SelectedTargetIds", out var targetIdsObj))
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                    return StateResult.Complete();
                }

                if (!_sessionManager.TryGetSession((int)sessionMsgIdObj, out var session))
                {
                    await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "Session expired.", true, cancellationToken: cancellationToken);
                    return StateResult.Complete();
                }

                stateData.Data.TryGetValue("TargetType", out var targetTypeObj);
                string targetType = (string)(targetTypeObj ?? "Users");

                targetType = (string)targetTypeObj;
                List<int> selectedDbIds = (List<int>)targetIdsObj;
                List<long> finalTargetTgIds = new List<long>();

                // Получаем финальный список ID
                if (targetType == "Users")
                {
                    // Просто конвертируем ID пользователей в их Telegram ID
                    finalTargetTgIds = selectedDbIds.Select(dbId => _userGetter.GetTelegramIDbyUserID(dbId)).ToList();
                }
                else // Groups
                {
                    // Для групп, нам нужно получить ВСЕХ участников каждой выбранной группы
                    HashSet<long> uniqueTgIds = new HashSet<long>();
                    foreach (int groupId in selectedDbIds)
                    {
                        List<int> memberDbIds = (List<int>)await _groupGetter.GetAllUsersIdsInGroup(groupId);
                        foreach (int memberDbId in memberDbIds)
                        {
                            uniqueTgIds.Add(_userGetter.GetTelegramIDbyUserID(memberDbId));
                        }
                    }
                    finalTargetTgIds = uniqueTgIds.ToList();
                }

                // Запускаем главный конвейер
                _ = _mediaFlow.StartFlow(botClient, update, session, finalTargetTgIds);

                await botClient.EditMessageText(chatId, session.StatusMessageId,
                    $"Starting distribution to {finalTargetTgIds.Count} selected targets...",
                    cancellationToken: cancellationToken);

                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);
                return StateResult.Complete();
                }

        return StateResult.Ignore();
    }

    private async Task<List<int>> ValidateUserIds(int actingUserId, List<int> inputIds)
    {
        var allowedTgIds = await _contactGetter.GetAllContactUserTGIds(actingUserId);
        return inputIds.Where(id => allowedTgIds.Contains(_userGetter.GetTelegramIDbyUserID(id))).ToList();
    }

    private async Task<List<int>> ValidateGroupIds(int actingUserId, List<int> inputIds)
    {
        var userGroups = await _groupGetter.GetGroupIDsByUserId(actingUserId);
        return inputIds.Where(id => userGroups.Contains(id)).ToList();
    }

    private async Task<List<int>?> ParseAndValidateIds(long chatId, string messageText, UserStateData stateData)
    {
        var actingUserId = _userGetter.GetUserIDbyTelegramID(chatId);
        stateData.Data.TryGetValue("TargetType", out var targetTypeObj);
        var targetType = (string)(targetTypeObj ?? "Users");
        
        List<int> inputIds;
        try 
        {
            inputIds = messageText.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            if (inputIds.Count == 0) return null;
        }
        catch 
        {
            return null;
        }

        var validTargetIds = targetType == "Users"
            ? await ValidateUserIds(actingUserId, inputIds)
            : await ValidateGroupIds(actingUserId, inputIds);
            
        if (validTargetIds.Count == 0)
        {
            return null;
        }
        
        return validTargetIds;
    }
}