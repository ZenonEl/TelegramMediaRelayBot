using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
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

    public string Name => "SelectTargets";

    public SelectTargetsStateHandler(
        IUserGetter userGetter,
        IContactGetter contactGetter,
        IGroupGetter groupGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _userGetter = userGetter;
        _contactGetter = contactGetter;
        _groupGetter = groupGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update)) return StateResult.Complete();
        
        var actingUserId = _userGetter.GetUserIDbyTelegramID(chatId);
        
        // Определяем, что мы выбираем (пользователей или группы), на основе данных,
        // переданных при запуске состояния.
        stateData.Data.TryGetValue("TargetType", out var targetTypeObj);
        var targetType = (string)(targetTypeObj ?? "Users");

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Ожидание списка ID
            // ========================================================================
            case 0:
                var messageText = update.Message?.Text;
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
                    // Логика показа списка доступных вариантов
                    if (targetType == "Users")
                    {
                        await ShowAvailableContacts(botClient, chatId, actingUserId, cancellationToken);
                    }
                    else // Groups
                    {
                        await ShowAvailableGroups(botClient, chatId, actingUserId, cancellationToken);
                    }
                    return StateResult.Continue();
                }

                var validTargetIds = targetType == "Users"
                    ? await ValidateUserIds(actingUserId, inputIds)
                    : await ValidateGroupIds(actingUserId, inputIds);

                if (validTargetIds.Count == 0)
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("NoValidTargetsFound"), cancellationToken: cancellationToken); // Нужна новая строка
                    return StateResult.Continue();
                }

                stateData.Data["SelectedTargetIds"] = validTargetIds;
                
                // TODO: Здесь мы должны как-то вернуться к основной сессии и передать ей эти ID.
                // Пока что просто завершаем и выводим сообщение.
                await botClient.SendMessage(chatId, $"Selected IDs: {string.Join(", ", validTargetIds)}", cancellationToken: cancellationToken);
                
                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
    
    // --- Вспомогательные методы, которые мы уже писали ---

    private async Task ShowAvailableContacts(ITelegramBotClient botClient, long chatId, int actingUserId, CancellationToken cancellationToken)
    {
        var tgIds = await _contactGetter.GetAllContactUserTGIds(actingUserId);
        var infos = tgIds.Select(tg => {
            int cid = _userGetter.GetUserIDbyTelegramID(tg);
            string uname = _userGetter.GetUserNameByTelegramID(tg);
            return string.Format(_resourceService.GetResourceString("ContactInfo"), cid, uname, "");
        }).ToList();
        var header = _resourceService.GetResourceString("YourContacts");
        var body = infos.Any() ? string.Join("\n", infos) : _resourceService.GetResourceString("NoUsersFound");
        await botClient.SendMessage(chatId, $"{header}\n{body}\n\n{_resourceService.GetResourceString("PleaseEnterContactIDs")}", cancellationToken: cancellationToken);
    }

    private async Task ShowAvailableGroups(ITelegramBotClient botClient, long chatId, int actingUserId, CancellationToken cancellationToken)
    {
        var groupIds = await _groupGetter.GetGroupIDsByUserId(actingUserId);
        var lines = await Task.WhenAll(groupIds.Select(async gid => $"{await _groupGetter.GetGroupNameById(gid)} (ID: {gid})"));
        var header = _resourceService.GetResourceString("YourGroups");
        var body = lines.Any() ? string.Join("\n", lines) : _resourceService.GetResourceString("NoGroupsFound");
        await botClient.SendMessage(chatId, $"{header}\n{body}\n\n{_resourceService.GetResourceString("PleaseEnterGroupIDs")}", cancellationToken: cancellationToken);
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
}