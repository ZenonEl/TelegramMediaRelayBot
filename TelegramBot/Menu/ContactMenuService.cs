// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IContactMenuService
{
    Task StartAddContactFlow(ITelegramBotClient botClient, Update update);
    Task StartDeleteContactFlow(ITelegramBotClient botClient, Update update);
    Task StartMuteContactFlow(ITelegramBotClient botClient, Update update);
    Task StartUnmuteContactFlow(ITelegramBotClient botClient, Update update);
    Task ViewContacts(ITelegramBotClient botClient, Update update);
    Task StartEditContactGroupFlow(ITelegramBotClient botClient, Update update);
}

public class ContactMenuService : IContactMenuService
{
    private readonly IUserStateManager _stateManager;
    private readonly IUserGetter _userGetter;
    private readonly IContactGetter _contactGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public ContactMenuService(
        IUserStateManager stateManager,
        IUserGetter userGetter,
        IContactGetter contactGetter,
        IGroupGetter groupGetter,
        IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _stateManager = stateManager;
        _userGetter = userGetter;
        _contactGetter = contactGetter;
        _groupGetter = groupGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public async Task StartAddContactFlow(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var newState = new UserStateData { StateName = "AddContact", Step = 0 };
        _stateManager.Set(chatId, newState);
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, _resourceService.GetResourceString("SpecifyContactLink"));
    }

    public async Task StartDeleteContactFlow(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var userId = _userGetter.GetUserIDbyTelegramID(chatId);
        
        var newState = new UserStateData { StateName = "RemoveContacts", Step = 0 };
        _stateManager.Set(chatId, newState);

        var tgIds = await _contactGetter.GetAllContactUserTGIds(userId);
        var infos = await Task.WhenAll(tgIds.Select(async tg =>
        {
            var id = _userGetter.GetUserIDbyTelegramID(tg);
            var uname = _userGetter.GetUserNameByTelegramID(tg);
            var membership = await BuildMembershipInfo(userId, id);
            return string.Format(_resourceService.GetResourceString("ContactInfo"), id, uname, "") + (string.IsNullOrEmpty(membership) ? "" : $"\n{membership}");
        }));
        
        var prompt = $"{_resourceService.GetResourceString("YourContacts")}\n{string.Join("\n", infos)}\n\n{_resourceService.GetResourceString("InputContactId")}";
        await botClient.SendMessage(chatId, prompt, cancellationToken: CancellationToken.None);
    }

    public async Task StartMuteContactFlow(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var userId = _userGetter.GetUserIDbyTelegramID(chatId);
        
        var newState = new UserStateData { StateName = "MuteUser", Step = 0 };
        _stateManager.Set(chatId, newState);

        var tgIds = await _contactGetter.GetAllContactUserTGIds(userId);
        var infos = await Task.WhenAll(tgIds.Select(async tg => {
            var id = _userGetter.GetUserIDbyTelegramID(tg);
            var uname = _userGetter.GetUserNameByTelegramID(tg);
            var membership = await BuildMembershipInfo(userId, id);
            return string.Format(_resourceService.GetResourceString("ContactInfo"), id, uname, "") + (string.IsNullOrEmpty(membership) ? "" : $"\n{membership}");
        }));
        
        var text = $"{_resourceService.GetResourceString("MuteUserInstructions")}\n\n{_resourceService.GetResourceString("YourContacts")}\n{string.Join("\n", infos)}";
        await botClient.SendMessage(chatId, text, cancellationToken: CancellationToken.None);
    }
    
    public async Task StartUnmuteContactFlow(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var userId = _userGetter.GetUserIDbyTelegramID(chatId);

        var newState = new UserStateData { StateName = "UnmuteUser", Step = 0 };
        _stateManager.Set(chatId, newState);
        
        var tgIds = await _contactGetter.GetAllContactUserTGIds(userId);
        var infos = await Task.WhenAll(tgIds.Select(async tg => {
            var id = _userGetter.GetUserIDbyTelegramID(tg);
            var uname = _userGetter.GetUserNameByTelegramID(tg);
            var membership = await BuildMembershipInfo(userId, id);
            return string.Format(_resourceService.GetResourceString("ContactInfo"), id, uname, "") + (string.IsNullOrEmpty(membership) ? "" : $"\n{membership}");
        }));

        var text = $"{_resourceService.GetResourceString("UnmuteUserInstructions")}\n\n{_resourceService.GetResourceString("YourContacts")}\n{string.Join("\n", infos)}";
        await botClient.SendMessage(chatId, text, cancellationToken: CancellationToken.None);
    }

    public async Task ViewContacts(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var userId = _userGetter.GetUserIDbyTelegramID(chatId);

        var contactUserTGIds = await _contactGetter.GetAllContactUserTGIds(userId);
        var contactUsersInfo = contactUserTGIds.Select(tgId => {
            var id = _userGetter.GetUserIDbyTelegramID(tgId);
            var username = _userGetter.GetUserNameByTelegramID(tgId);
            return string.Format(_resourceService.GetResourceString("ContactInfo"), id, username, "");
        }).ToList();

        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), CancellationToken.None, $"{_resourceService.GetResourceString("YourContacts")}\n{string.Join("\n", contactUsersInfo)}");
    }

    public async Task StartEditContactGroupFlow(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var userId = _userGetter.GetUserIDbyTelegramID(chatId);

        var newState = new UserStateData { StateName = "EditContactGroup" };
        _stateManager.Set(chatId, newState);

        var groupInfos = await UsersGroup.GetUserGroupInfoByUserId(userId, _groupGetter);
        var messageText = groupInfos.Any()
            ? $"{_resourceService.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}"
            : _resourceService.GetResourceString("AltYourGroupsText");

        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, string.Format(_resourceService.GetResourceString("ContactGroupInfoText"), messageText));
    }

    private async Task<string> BuildMembershipInfo(int ownerUserId, int contactUserId)
    {
        var groupIds = await _groupGetter.GetGroupIDsByUserId(ownerUserId);
        var membership = new List<string>();
        foreach (var gid in groupIds)
        {
            var members = await _groupGetter.GetAllUsersIdsInGroup(gid);
            if (members.Contains(contactUserId))
            {
                string name = await _groupGetter.GetGroupNameById(gid);
                membership.Add($"{name} (ID: {gid})");
            }
        }
        if (membership.Count == 0) return string.Empty;
        return $"<i>{_resourceService.GetResourceString("ContactGroupsLabel")}</i> {string.Join(", ", membership)}";
    }
}