using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using DataBase;
using TelegramMediaRelayBot;

namespace MediaTelegramBot.Menu;

public class Groups
{
    public static async Task ViewGroups(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);

        TelegramBot.userStates[chatId] = new ProcessUsersGroupState();
        List<string> groupInfos = UsersGroup.GetUserGroupInfoByUserId(userId);

        string messageText = groupInfos.Any() 
            ? $"{Config.GetResourceString("YourGroupsText")}\n{string.Join("\n", groupInfos)}" 
            : Config.GetResourceString("AltYourGroupsText");

        await Utils.Utils.SendMessage(
            botClient,
            update,
            UsersGroup.GetUsersGroupActionsKeyboardMarkup(groupInfos.Count > 0),
            cancellationToken,
            messageText
        );
    }
}