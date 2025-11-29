// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.


using System.Text;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Models;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IGroupMenuService
{
    Task ShowGroupsMenu(ITelegramBotClient botClient, Update update);
    Task<List<GroupViewModel>> GetGroupsForDisplay(int userId);
    Task ShowAvailableGroups(ITelegramBotClient botClient, Update update);

}

public class GroupMenuService : IGroupMenuService
{
    private readonly IUserStateManager _stateManager;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public GroupMenuService(
        IUserStateManager stateManager,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _stateManager = stateManager;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public async Task ShowGroupsMenu(ITelegramBotClient botClient, Update update)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;
        var userId = _userGetter.GetUserIDbyTelegramID(chatId);

        // Запускаем универсальное состояние для управления группами
        var newState = new UserStateData { StateName = "ManageGroups" }; // Имя нового универсального обработчика
        _stateManager.Set(chatId, newState);

        var groupInfos = await UsersGroup.GetUserGroupInfoByUserId(userId, _groupGetter);

        var messageText = groupInfos.Any()
            ? $"{_resourceService.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}"
            : _resourceService.GetResourceString("AltYourGroupsText");

        await _interactionService.ReplyToUpdate(
            botClient,
            update,
            UsersGroup.GetUsersGroupActionsKeyboardMarkup(groupInfos.Any()),
            CancellationToken.None,
            messageText
        );
    }

    public async Task<List<GroupViewModel>> GetGroupsForDisplay(int userId)
    {
        var groupIds = await _groupGetter.GetGroupIDsByUserId(userId);
        var groupViewModels = new List<GroupViewModel>();

        foreach (var groupId in groupIds)
        {
            groupViewModels.Add(new GroupViewModel
            {
                Id = groupId,
                Name = await _groupGetter.GetGroupNameById(groupId),
                Description = await _groupGetter.GetGroupDescriptionById(groupId),
                MemberCount = await _groupGetter.GetGroupMemberCount(groupId),
                IsDefault = await _groupGetter.GetIsDefaultGroup(groupId)
            });
        }
        return groupViewModels;
    }

    public async Task ShowAvailableGroups(ITelegramBotClient botClient, Update update)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;
        var userId = _userGetter.GetUserIDbyTelegramID(chatId);

        // 1. Получаем СТРУКТУРИРОВАННЫЕ ДАННЫЕ
        List<GroupViewModel> groups = await GetGroupsForDisplay(userId);

        // 2. ФОРМАТИРУЕМ в красивое сообщение
        var sb = new StringBuilder();
        sb.AppendLine(_resourceService.GetResourceString("YourGroups"));
        
        if (groups.Any())
        {
            foreach (var group in groups)
            {
                sb.AppendLine($"{group.Name} (ID: {group.Id})");
            }
        }
        else
        {
            sb.AppendLine(_resourceService.GetResourceString("NoGroupsFound"));
        }
        sb.AppendLine($"\n{_resourceService.GetResourceString("PleaseEnterGroupIDs")}"); //TODO поправить вид сообщения с айди и его удаление
        
        // 3. ОТПРАВЛЯЕМ сообщение
        await botClient.SendMessage(chatId, sb.ToString(), cancellationToken: CancellationToken.None);
    }
}