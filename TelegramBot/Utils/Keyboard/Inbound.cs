using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;
using TelegramMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class InBoundKB
{
    public static InlineKeyboardMarkup GetInBoundActionsKeyboardMarkup(string userId, string callbackData)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("AcceptButtonText"), $"user_accept_inbounds_invite:{userId}"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Config.GetResourceString("DeclineButtonText"), $"user_decline_inbounds_invite:{userId}"),
                        },
                        new[]
                        {
                            KeyboardUtils.GetReturnButton(callbackData)
                        },
                    });
        return inlineKeyboard;
    }

    public static InlineKeyboardMarkup GetInboundsKeyboardMarkup(Update update)
    {
        var buttonDataList = DBforInbounds.GetInboundsButtonData(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));

        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

        foreach (var buttonData in buttonDataList)
        {
            var button = InlineKeyboardButton.WithCallbackData(buttonData.ButtonText, buttonData.CallbackData);
            inlineKeyboardButtons.Add(new[] { button });
        }
        inlineKeyboardButtons.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }
}
