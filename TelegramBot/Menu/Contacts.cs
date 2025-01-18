using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using DataBase;
using TelegramMediaRelayBot;

namespace MediaTelegramBot.Menu;

public class Contacts
{
    public static CancellationToken cancellationToken = TelegramBot.cancellationToken;

    public static async Task AddContact(ITelegramBotClient botClient, Update update, long chatId)
    {
        TelegramBot.userStates[chatId] = new ProcessContactState();
        await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, Config.GetResourceString("SpecifyContactLink"));
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
        List<long> contactUserTGIds = await CoreDB.GetAllContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.CallbackQuery!.Message!.Chat.Id));
        List<string> contactUsersInfo = new List<string>();

        foreach (var contactUserId in contactUserTGIds)
        {
            int id = DBforGetters.GetUserIDbyTelegramID(contactUserId);
            string username = DBforGetters.GetUserNameByTelegramID(contactUserId);
            string link = DBforGetters.GetSelfLink(contactUserId);

            contactUsersInfo.Add(string.Format(Config.GetResourceString("ContactInfo"), id, username, link));
        }

        await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), cancellationToken, $"{Config.GetResourceString("YourContacts")}\n{string.Join("\n", contactUsersInfo)}");
    }

    public static async Task EditContactGroup(ITelegramBotClient botClient, Update update, long chatId)
    {
        TelegramBot.userStates[chatId] = new ProcessContactGroupState();

        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        List<string> groupInfos = UsersGroup.GetUserGroupInfoByUserId(userId);

        string messageText = groupInfos.Any() 
            ? $"{Config.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
            : Config.GetResourceString("AltYourGroupsText");

        await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken, string.Format(Config.GetResourceString("ContactGroupInfoText"), messageText));
    }
}