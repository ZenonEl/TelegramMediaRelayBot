using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using DataBase;
using TikTokMediaRelayBot;

namespace MediaTelegramBot.Menu;

public class Contacts
{
    public static CancellationToken cancellationToken = TelegramBot.cancellationToken;

    public static Task AddContact(ITelegramBotClient botClient, Update update)
    {
        return Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, Config.GetResourceString("SpecifyContactLink"));
    }

    public static async Task MuteUserContact(ITelegramBotClient botClient, Update update, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, Config.GetResourceString("MuteUserInstructions"), cancellationToken: cancellationToken);
        TelegramBot.userStates[chatId] = new ProcessUserMuteState();
    }

    public static async Task UnMuteUserContact(ITelegramBotClient botClient, Update update, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery!.Message!.Chat.Id, Config.GetResourceString("UnmuteUserInstructions"), cancellationToken: cancellationToken);
        TelegramBot.userStates[chatId] = new ProcessUserUnMuteState();
    }

    public static async Task ViewContacts(ITelegramBotClient botClient, Update update)
    {
        var contactUserTGIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));
        var contactUsersInfo = new List<string>();

        foreach (var contactUserId in contactUserTGIds)
        {
            int id = DBforGetters.GetUserIDbyTelegramID(contactUserId);
            string username = DBforGetters.GetUserNameByTelegramID(contactUserId);
            string link = DBforGetters.GetSelfLink(contactUserId);

            contactUsersInfo.Add(string.Format(Config.GetResourceString("ContactInfo"), id, username, link));
        }

        await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), cancellationToken, $"{Config.GetResourceString("YourContacts")}\n{string.Join("\n", contactUsersInfo)}");
    }
}