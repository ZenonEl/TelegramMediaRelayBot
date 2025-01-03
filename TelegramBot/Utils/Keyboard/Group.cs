using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;
using TelegramMediaRelayBot;


namespace MediaTelegramBot.Utils;

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
                        InlineKeyboardButton.WithCallbackData("Поменять название", $"user_change_group_name:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Поменять описание", $"user_change_group_description:{groupId}"),
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
}