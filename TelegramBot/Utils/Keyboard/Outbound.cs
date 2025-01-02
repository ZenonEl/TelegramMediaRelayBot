using Telegram.Bot.Types.ReplyMarkups;
using DataBase;
using TelegramMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class OutBoundKB
{
    public static InlineKeyboardMarkup GetOutboundKeyboardMarkup(long userId)
    {
        var buttonDataList = DBforOutbound.GetOutboundButtonData(DBforGetters.GetUserIDbyTelegramID(userId));

        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (var buttonData in buttonDataList)
        {
            var button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    public static InlineKeyboardMarkup GetOutboundActionsKeyboardMarkup(string userId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DeclineButtonText"), $"revoke_outbound_invite:{userId}"),
                        },
                        new[]
                        {
                            KeyboardUtils.GetReturnButton()
                        },
                    });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetOutBoundActionsKeyboardMarkup(string userId, string callbackData)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("YesButtonText"), $"user_accept_revoke_outbound_invite:{userId}"),
                        },
                        new[]
                        {
                            KeyboardUtils.GetReturnButton(callbackData)
                        },
                    });
        return inlineKeyboard;
    }
}