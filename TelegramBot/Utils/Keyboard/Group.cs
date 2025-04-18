// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using Telegram.Bot.Types.ReplyMarkups;


namespace TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

public static class UsersGroup
{
    public static InlineKeyboardMarkup GetUsersGroupActionsKeyboardMarkup(bool groupsMoreZero)
    {
        var kb = new List<List<InlineKeyboardButton>>
        {
            new[]
        {
            InlineKeyboardButton.WithCallbackData(
                Config.GetResourceString("CreateGroupButtonText"),
                "user_create_group"
            )
        }.ToList()
        };

        if (groupsMoreZero)
        {
            kb.Add(new[] 
            { 
                InlineKeyboardButton.WithCallbackData(
                    Config.GetResourceString("EditGroupButtonText"), 
                    "user_edit_group"
                ) 
            }.ToList());

            kb.Add(new[] 
            { 
                InlineKeyboardButton.WithCallbackData(
                    Config.GetResourceString("DeleteGroupButtonText"), 
                    "user_delete_group"
                ) 
            }.ToList());
        }
        kb.Add(                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    }.ToList());

        return new InlineKeyboardMarkup(kb);
    }

    public static InlineKeyboardMarkup GetUsersGroupEditActionsKeyboardMarkup(int groupId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ChangeNameText"), $"user_change_group_name:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ChangeDescriptionText"), $"user_change_group_description:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ChangeIsDefaultEnabledText"), $"user_change_is_default:{groupId}"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
    }

    public static List<string> GetUserGroupInfoByUserId(int userId)
    {
        List<int> groupIds = DBforGroups.GetGroupIDsByUserId(userId);

        var groupInfos = new List<string>();
        string groupInfo;
        foreach (var groupId in groupIds)
        {
            string groupName = DBforGroups.GetGroupNameById(groupId);

            string groupDescription = DBforGroups.GetGroupDescriptionById(groupId);

            int memberCount = DBforGroups.GetGroupMemberCount(groupId);
            bool isDefault = DBforGroups.GetIsDefaultGroup(groupId);
            groupInfo = string.Format(
                Config.GetResourceString("GroupInfoText"), 
                DBforGroups.GetGroupNameById(groupId),
                groupId,
                DBforGroups.GetGroupDescriptionById(groupId),
                DBforGroups.GetGroupMemberCount(groupId),
                DBforGroups.GetIsDefaultGroup(groupId)
            );
            groupInfos.Add(groupInfo);
        }
        return groupInfos;
    }

    public static string GetUserGroupInfoByGroupId(int groupId)
    {
        return string.Format(Config.GetResourceString("GroupInfoText"), DBforGroups.GetGroupNameById(groupId),
                                                groupId,
                                                DBforGroups.GetGroupDescriptionById(groupId), DBforGroups.GetGroupMemberCount(groupId), 
                                                DBforGroups.GetIsDefaultGroup(groupId));
    }
}