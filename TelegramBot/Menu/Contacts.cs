// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using Telegram.Bot.Types;
using TelegramMediaRelayBot.TelegramBot.Utils ;

using TelegramMediaRelayBot;

namespace MediaTelegramBot.Menu;

public class Contacts
{
    public static CancellationToken cancellationToken = TelegramBot.cancellationToken;

    public static async Task AddContact(ITelegramBotClient botClient, Update update, long chatId)
    {
        TelegramBot.userStates[chatId] = new ProcessContactState();
        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, Config.GetResourceString("SpecifyContactLink"));
    }

    public static async Task DeleteContact(ITelegramBotClient botClient, Update update, long chatId)
    {
        Message statusMessage = await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, "Укажите айди контактов для удаления:", cancellationToken: cancellationToken);
        TelegramBot.userStates[chatId] = new ProcessRemoveUser(statusMessage);
    }

    public static async Task MuteUserContact(ITelegramBotClient botClient, Update update, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, Config.GetResourceString("MuteUserInstructions"), cancellationToken: cancellationToken);
        TelegramBot.userStates[chatId] = new ProcessUserMuteState();
    }

    public static async Task UnMuteUserContact(ITelegramBotClient botClient, Update update, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, Config.GetResourceString("UnmuteUserInstructions"), cancellationToken: cancellationToken);
        TelegramBot.userStates[chatId] = new ProcessUserUnMuteState();
    }

    public static async Task ViewContacts(ITelegramBotClient botClient, Update update)
    {
        List<long> contactUserTGIds = await ContactGetter.GetAllContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));
        List<string> contactUsersInfo = new List<string>();

        foreach (var contactUserId in contactUserTGIds)
        {
            int id = DBforGetters.GetUserIDbyTelegramID(contactUserId);
            string username = DBforGetters.GetUserNameByTelegramID(contactUserId);
            string link = DBforGetters.GetSelfLink(contactUserId);

            contactUsersInfo.Add(string.Format(Config.GetResourceString("ContactInfo"), id, username, link));
        }

        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), cancellationToken, $"{Config.GetResourceString("YourContacts")}\n{string.Join("\n", contactUsersInfo)}");
    }

    public static async Task EditContactGroup(ITelegramBotClient botClient, Update update, long chatId)
    {
        TelegramBot.userStates[chatId] = new ProcessContactGroupState();

        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        List<string> groupInfos = UsersGroup.GetUserGroupInfoByUserId(userId);

        string messageText = groupInfos.Any() 
            ? $"{Config.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
            : Config.GetResourceString("AltYourGroupsText");

        await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(Config.GetResourceString("ContactGroupInfoText"), messageText));
    }
}