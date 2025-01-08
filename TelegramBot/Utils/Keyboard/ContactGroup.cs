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
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("AddContactsButtonText"), $"user_add_contact_to_group:{groupId}"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Config.GetResourceString("RemoveContactsButtonText"), $"user_remove_contact_from_group:{groupId}"),
                    },
                    new[]
                    {
                        KeyboardUtils.GetReturnButton()
                    },
                });
        return inlineKeyboard;
    }
}