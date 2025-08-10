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
        TGBot.StateManager.Set(chatId, new ProcessContactState(contactRepository, contactGetter, userGetter, privacySettingsGetter, resourceService));
        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, GetResourceString("SpecifyContactLink"));
    }

    public static async Task DeleteContact(ITelegramBotClient botClient, Update update, long chatId, IContactRemover contactRemoverRepository, IContactGetter contactGetterRepository, IUserGetter userGetter, TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        // Show user's contacts to help choose IDs
        List<long> tgIds = await contactGetterRepository.GetAllContactUserTGIds(userGetter.GetUserIDbyTelegramID(chatId));
        List<string> infos = new();
        foreach (var tg in tgIds)
        {
            int id = userGetter.GetUserIDbyTelegramID(tg);
            string uname = userGetter.GetUserNameByTelegramID(tg);
            string link = userGetter.GetUserSelfLink(tg);
            infos.Add(string.Format(GetResourceString("ContactInfo"), id, uname, link));
        }
        string prompt = $"{GetResourceString("YourContacts")}\n{string.Join("\n", infos)}\n\n{GetResourceString("InputContactId")}";
        Message statusMessage = await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, prompt, cancellationToken: cancellationToken);
        TGBot.StateManager.Set(chatId, new ProcessRemoveUser(statusMessage, contactRemoverRepository, contactGetterRepository, userGetter, resourceService));
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
        // Send instructions with contact list to simplify ID selection
        List<long> tgIds = await contactGetterRepository.GetAllContactUserTGIds(userGetter.GetUserIDbyTelegramID(chatId));
        List<string> infos = new();
        foreach (var tg in tgIds)
        {
            int id = userGetter.GetUserIDbyTelegramID(tg);
            string uname = userGetter.GetUserNameByTelegramID(tg);
            string link = userGetter.GetUserSelfLink(tg);
            infos.Add(string.Format(GetResourceString("ContactInfo"), id, uname, link));
        }
        string text = $"{GetResourceString("MuteUserInstructions")}\n\n{GetResourceString("YourContacts")}\n{string.Join("\n", infos)}";
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, text, cancellationToken: cancellationToken);
        TGBot.StateManager.Set(chatId, new ProcessUserMuteState(contactAdderRepository, contactGetterRepository, userGetter, resourceService));
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
        // Show user's contacts to simplify choosing whom to unmute
        List<long> tgIds2 = await contactGetter.GetAllContactUserTGIds(userGetter.GetUserIDbyTelegramID(chatId));
        List<string> infos2 = new();
        foreach (var tg in tgIds2)
        {
            int id = userGetter.GetUserIDbyTelegramID(tg);
            string uname = userGetter.GetUserNameByTelegramID(tg);
            string link = userGetter.GetUserSelfLink(tg);
            infos2.Add(string.Format(GetResourceString("ContactInfo"), id, uname, link));
        }
        string text2 = $"{GetResourceString("UnmuteUserInstructions")}\n\n{GetResourceString("YourContacts")}\n{string.Join("\n", infos2)}";
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, text2, cancellationToken: cancellationToken);
        TGBot.StateManager.Set(chatId, new ProcessUserUnMuteState(contactRemoverRepository, contactGetter, userGetter, resourceService));
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
        IContactGetter contactGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        TGBot.StateManager.Set(chatId, new ProcessContactGroupState(contactGroupRepository, userGetter, groupGetter, contactGetter, resourceService));

        int userId = userGetter.GetUserIDbyTelegramID(chatId);
        List<string> groupInfos = await UsersGroup.GetUserGroupInfoByUserId(userId, groupGetter);

        string messageText = groupInfos.Any() 
            ? $"{GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
            : GetResourceString("AltYourGroupsText");

        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(GetResourceString("ContactGroupInfoText"), messageText));
    }
}