using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using DataBase;
using TikTokMediaRelayBot;

namespace MediaTelegramBot.Menu;

public class Contacts
{
    public static Task AddContact(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        return Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, Config.resourceManager.GetString("SpecifyContactLink", System.Globalization.CultureInfo.CurrentUICulture));
    }

    public static async Task MuteUserContact(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, Config.resourceManager.GetString("MuteUserInstructions", System.Globalization.CultureInfo.CurrentUICulture), cancellationToken: cancellationToken);
        TelegramBot.userStates[chatId] = new ProcessUserMuteState();
    }

    public static async Task UnMuteUserContact(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        await botClient.SendMessage(update.CallbackQuery.Message.Chat.Id, Config.resourceManager.GetString("UnmuteUserInstructions", System.Globalization.CultureInfo.CurrentUICulture), cancellationToken: cancellationToken);
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

            contactUsersInfo.Add(string.Format(Config.resourceManager.GetString("ContactInfo", System.Globalization.CultureInfo.CurrentUICulture), id, username, link));
        }

        await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), cancellationToken, $"{Config.resourceManager.GetString("YourContacts", System.Globalization.CultureInfo.CurrentUICulture)}\n{string.Join("\n", contactUsersInfo)}");
    }
}