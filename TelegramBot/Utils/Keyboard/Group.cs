using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;
using TelegramMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class UsersGroup
{
    public static InlineKeyboardMarkup GetUsersGroupActionsKeyboardMarkup()
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("CreateGroupButtonText"), $"user_create_group"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("EditGroupButtonText"), $"user_edit_group"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DeleteGroupButtonText"), $"user_delete_group"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
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
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("ChangeIsDefaultEnabled"), $"user_change_is_default:{groupId}"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
    }
}