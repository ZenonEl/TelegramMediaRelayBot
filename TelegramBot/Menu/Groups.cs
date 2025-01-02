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
        List<int> groupIds = DBforGroups.GetGroupIdByUserId(userId);

        var groupInfos = new List<string>();

        foreach (var groupId in groupIds)
        {
            string groupName = DBforGroups.GetGroupNameById(groupId);

            string groupDescription = DBforGroups.GetGroupDescriptionById(groupId);

            int memberCount = DBforGroups.GetGroupMemberCount(groupId);
            bool isDefault = DBforGroups.GetIsDefaultGroup(groupId);

            groupInfos.Add($@"📌 <b>Название:</b> {groupName}
    🆔 <b>ID:</b> {groupId}
    📝 <b>Описание:</b> {groupDescription}
    👥 <b>Участников:</b> {memberCount}
    🌟 <b>По умолчанию:</b> {isDefault}
    --------------------------");
        }

        string messageText = groupInfos.Any() 
            ? $"{Config.GetResourceString("YourGroups")}\n\n{string.Join("\n", groupInfos)}" 
            : "У вас пока нет групп.";

        await Utils.Utils.SendMessage(
            botClient,
            update,
            UsersGroup.GetUsersGroupActionsKeyboardMarkup(),
            cancellationToken,
            messageText
        );
    }
}