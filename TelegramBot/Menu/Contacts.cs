using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using DataBase;


namespace MediaTelegramBot.Menu;



public class Contacts
{
    public static async Task MuteUserContact(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Чтобы замутить человека (вы не будете получать от него видео) вам нужно указать либо его ID либо его ссылку");
        TelegramBot.userStates[chatId] = new ProcessUserMuteState();
    }
}