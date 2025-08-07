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
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.Menu;

public class Contacts
{
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    public static CancellationToken cancellationToken = TGBot.cancellationToken;
    
    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }

    public static async Task AddContact(ITelegramBotClient botClient, Update update, long chatId, IContactAdder contactRepository, IContactGetter contactGetter, IUserGetter userGetter, IPrivacySettingsGetter privacySettingsGetter, TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        TGBot.userStates[chatId] = new ProcessContactState(contactRepository, contactGetter, userGetter, privacySettingsGetter, resourceService);
        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, GetResourceString("SpecifyContactLink"));
    }

    public static async Task DeleteContact(ITelegramBotClient botClient, Update update, long chatId, IContactRemover contactRemoverRepository, IContactGetter contactGetterRepository, IUserGetter userGetter, TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        Message statusMessage = await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, GetResourceString("InputContactId"), cancellationToken: cancellationToken);
        TGBot.userStates[chatId] = new ProcessRemoveUser(statusMessage, contactRemoverRepository, contactGetterRepository, userGetter, resourceService);
    }

    public static async Task MuteUserContact(
        ITelegramBotClient botClient,
        Update update,
        long chatId,
        IContactAdder contactAdderRepository,
        IContactGetter contactGetterRepository,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, GetResourceString("MuteUserInstructions"), cancellationToken: cancellationToken);
        TGBot.userStates[chatId] = new ProcessUserMuteState(contactAdderRepository, contactGetterRepository, userGetter, resourceService);
    }

    public static async Task UnMuteUserContact(
        ITelegramBotClient botClient,
        Update update,
        long chatId,
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetter,
        IUserGetter userGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, GetResourceString("UnmuteUserInstructions"), cancellationToken: cancellationToken);
        TGBot.userStates[chatId] = new ProcessUserUnMuteState(contactRemoverRepository, contactGetter, userGetter, resourceService);
    }

    public static async Task ViewContacts(ITelegramBotClient botClient, Update update, IContactGetter contactGetterRepository, IUserGetter userGetter)
    {
        List<long> contactUserTGIds = await contactGetterRepository.GetAllContactUserTGIds(userGetter.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));
        List<string> contactUsersInfo = new List<string>();

        foreach (var contactUserId in contactUserTGIds)
        {
            int id = userGetter.GetUserIDbyTelegramID(contactUserId);
            string username = userGetter.GetUserNameByTelegramID(contactUserId);
            string link = userGetter.GetUserSelfLink(contactUserId);

            contactUsersInfo.Add(string.Format(GetResourceString("ContactInfo"), id, username, link));
        }

        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), cancellationToken, $"{GetResourceString("YourContacts")}\n{string.Join("\n", contactUsersInfo)}");
    }

    public static async Task EditContactGroup(
        ITelegramBotClient botClient,
        Update update,
        long chatId,
        IContactGroupRepository contactGroupRepository,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        TGBot.userStates[chatId] = new ProcessContactGroupState(contactGroupRepository, userGetter, groupGetter, resourceService);

        int userId = userGetter.GetUserIDbyTelegramID(chatId);
        List<string> groupInfos = await UsersGroup.GetUserGroupInfoByUserId(userId, groupGetter);

        string messageText = groupInfos.Any() 
            ? $"{GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
            : GetResourceString("AltYourGroupsText");

        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(GetResourceString("ContactGroupInfoText"), messageText));
    }
}