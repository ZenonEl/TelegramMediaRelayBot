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

public class Groups
{
    public static async Task ViewGroups(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        IGroupSetter groupSetter)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = userGetter.GetUserIDbyTelegramID(chatId);

        TGBot.userStates[chatId] = new ProcessUsersGroupState(userGetter, groupGetter, groupSetter);
        List<string> groupInfos = await UsersGroup.GetUserGroupInfoByUserId(userId, groupGetter);

        string messageText = groupInfos.Any() 
            ? $"{Config.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
            : Config.GetResourceString("AltYourGroupsText");

        await CommonUtilities.SendMessage(
            botClient,
            update,
            UsersGroup.GetUsersGroupActionsKeyboardMarkup(groupInfos.Count > 0),
            cancellationToken,
            messageText
        );
    }
}