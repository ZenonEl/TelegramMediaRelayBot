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
    public static async Task UnMuteUserContact(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, "Чтобы размутить человека (вы снова будете получать от него видео) вам нужно указать либо его ID либо его ссылку");
        TelegramBot.userStates[chatId] = new ProcessUserUnMuteState();
    }

    public static async Task ViewContacts(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var contactUserTGIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery.Message.Chat.Id));
        var contactUsersInfo = new List<string>();
        
        foreach (var contactUserId in contactUserTGIds)
        {
            int id = DBforGetters.GetUserIDbyTelegramID(contactUserId);
            string username = await DBforGetters.GetUserNameByTelegramID(contactUserId);
            string link = DBforGetters.GetSelfLink(contactUserId);

            contactUsersInfo.Add($"\nПользователь с ID: {id}\nИменем: {username}\nСсылкой: <code>{link}</>");
        }
        await Utils.SendMessage(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), cancellationToken, $"Ваши контакты:\n{string.Join("\n", contactUsersInfo)}");
    }
}