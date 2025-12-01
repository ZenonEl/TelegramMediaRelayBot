// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

public static class UsersGroup
{
    private static readonly System.Resources.ResourceManager _resourceManager =
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);

    private static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }

    public static InlineKeyboardMarkup GetUsersGroupActionsKeyboardMarkup(bool groupsMoreZero)
    {
        var kb = new List<List<InlineKeyboardButton>>
        {
            new[]
        {
            InlineKeyboardButton.WithCallbackData(
                GetResourceString("CreateGroupButtonText"),
                "user_create_group"
            )
        }.ToList()
        };

        if (groupsMoreZero)
        {
            kb.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("EditGroupButtonText"),
                    "user_edit_group"
                )
            }.ToList());

            kb.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GetResourceString("DeleteGroupButtonText"),
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
                        InlineKeyboardButton.WithCallbackData(GetResourceString("ChangeNameText"), $"user_change_group_name:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetResourceString("ChangeDescriptionText"), $"user_change_group_description:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetResourceString("ChangeIsDefaultEnabledText"), $"user_change_is_default:{groupId}"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
    }

    public static async Task<List<string>> GetUserGroupInfoByUserId(int userId, IGroupGetter groupGetter)
    {
        IEnumerable<int> groupIds = await groupGetter.GetGroupIDsByUserId(userId);

        var groupInfos = new List<string>();
        string groupInfo;
        foreach (var groupId in groupIds)
        {
            string groupName = await groupGetter.GetGroupNameById(groupId);

            string groupDescription = await groupGetter.GetGroupDescriptionById(groupId);

            int memberCount = await groupGetter.GetGroupMemberCount(groupId);
            bool isDefault = await groupGetter.GetIsDefaultGroup(groupId);
            groupInfo = string.Format(
                GetResourceString("GroupInfoText"),
                await groupGetter.GetGroupNameById(groupId),
                groupId,
                await groupGetter.GetGroupDescriptionById(groupId),
                await groupGetter.GetGroupMemberCount(groupId),
                await groupGetter.GetIsDefaultGroup(groupId)
            );
            groupInfos.Add(groupInfo);
        }
        return groupInfos;
    }

    public static async Task<string> GetUserGroupInfoByGroupId(int groupId, IGroupGetter groupGetter)
    {
        return string.Format(GetResourceString("GroupInfoText"), await groupGetter.GetGroupNameById(groupId),
                                                groupId,
                                                await groupGetter.GetGroupDescriptionById(groupId),
                                                await groupGetter.GetGroupMemberCount(groupId),
                                                await groupGetter.GetIsDefaultGroup(groupId));
    }
}
