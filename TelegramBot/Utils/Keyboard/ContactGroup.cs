using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class ContactGroup
{

    public static InlineKeyboardMarkup GetContactGroupEditActionsKeyboardMarkup(int groupId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Добавить участника(ов)", $"user_add_contact_to_group:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Удалить участника(ов)", $"user_remove_contact_from_group:{groupId}"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
    }
}