using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;


namespace MediaTelegramBot.Utils;

public static class ReplyKeyboardUtils
{

    public static ReplyKeyboardMarkup GetSingleButtonKeyboardMarkup(string text)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton(text)
        })
        {
            ResizeKeyboard = true
        };
        return replyKeyboard;
    }

    public async static Task RemoveReplyMarkup(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var sentMessage = await botClient.SendMessage(chatId, "ㅤ", cancellationToken: cancellationToken, replyMarkup: new ReplyKeyboardRemove()); 
        await botClient.DeleteMessage(chatId, sentMessage.MessageId, cancellationToken);
    }
}