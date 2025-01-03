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
        List<int> groupIds = DBforGroups.GetGroupIDsByUserId(userId);

        var groupInfos = new List<string>();
        string groupInfo;
        foreach (var groupId in groupIds)
        {
            string groupName = DBforGroups.GetGroupNameById(groupId);

            string groupDescription = DBforGroups.GetGroupDescriptionById(groupId);

            int memberCount = DBforGroups.GetGroupMemberCount(groupId);
            bool isDefault = DBforGroups.GetIsDefaultGroup(groupId);
            groupInfo = string.Format(Config.GetResourceString("GroupInfoText"), DBforGroups.GetGroupNameById(groupId),
                                        DBforGroups.GetGroupDescriptionById(groupId), DBforGroups.GetGroupMemberCount(groupId), 
                                        DBforGroups.GetIsDefaultGroup(groupId));
            groupInfos.Add(groupInfo);
        }

        string messageText = groupInfos.Any() 
            ? $"{Config.GetResourceString("YourGroupsText")}\n\n{string.Join("\n", groupInfos)}" 
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