using Telegram.Bot;
using Telegram.Bot.Types;
using DataBase;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;


namespace MediaTelegramBot;

public class ProcessUsersGroupState : IUserState
{
    public UsersGroupState currentState;

    public string groupInfo = "";

    private string action = "";
    private string backCallback = "";
    private string groupName = "";
    private string description = "";
    private int groupId = 0;
    private bool isDBActionSuccessful = false;

    public ProcessUsersGroupState()
    {
        currentState = UsersGroupState.ProcessAction;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

        if (!TelegramBot.userStates.TryGetValue(chatId, out IUserState? value))
        {
            return;
        }

        var userState = (ProcessUsersGroupState)value;

        switch (userState.currentState)
        {
            case UsersGroupState.ProcessAction:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId)) return;
                groupInfo = string.Format(Config.GetResourceString("GroupInfoText"), DBforGroups.GetGroupNameById(groupId),
                                            DBforGroups.GetGroupDescriptionById(groupId), DBforGroups.GetGroupMemberCount(groupId), 
                                            DBforGroups.GetIsDefaultGroup(groupId));
                bool? isCallbackSuccessful = await ProcessCallbackData(botClient, update, cancellationToken);
                if (isCallbackSuccessful == true)
                {
                    userState.currentState = UsersGroupState.ProcessData;
                }
                else if (isCallbackSuccessful == null)
                {
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetReturnButtonMarkup(),
                        cancellationToken,
                        Config.GetResourceString("InputErrorMessage"));
                }

                break;

            case UsersGroupState.ProcessData:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId)) return;
                if (update.CallbackQuery != null && update.CallbackQuery.Data == backCallback)
                {
                    userState.currentState = UsersGroupState.ProcessAction;
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        $"{groupInfo}\n{Config.GetResourceString("ChooseOptionText")}");
                    return;
                }
                bool? isActionSuccessful = await ProcessAction(botClient, update, cancellationToken);
                if (isActionSuccessful == true) 
                {
                    userState.currentState = UsersGroupState.Finish;
                    return;
                }
                else if (isActionSuccessful == null)
                {
                    await botClient.SendMessage(chatId, Config.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken, replyMarkup: KeyboardUtils.GetReturnButtonMarkup());
                    return;
                }
                userState.currentState = UsersGroupState.ProcessAction;
                break;

            case UsersGroupState.Finish:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId)) return;
                if (update.CallbackQuery != null && update.CallbackQuery.Data == backCallback)
                {
                    userState.currentState = UsersGroupState.ProcessAction;
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        $"{groupInfo}\n{Config.GetResourceString("ChooseOptionText")}");
                    return;
                }
                ProcessFinish(chatId);
                string text = isDBActionSuccessful ? Config.GetResourceString("SuccessActionResult") : Config.GetResourceString("ErrorActionResult");
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken, text);
                TelegramBot.userStates.Remove(chatId);
                break;
        }
    }

    public async Task<bool?> ProcessCallbackData(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery == null) return null;
        action = update.CallbackQuery!.Data!;
        switch (action)
        {
            case "user_create_group":
                await Utils.Utils.SendMessage(botClient,
                                                update,
                                                KeyboardUtils.GetReturnButtonMarkup(),
                                                cancellationToken,
                                                Config.GetResourceString("NewGroupText"));
                return true;
            case "user_edit_group":
                backCallback = action;
                await botClient.SendMessage(update.CallbackQuery.Message!.Chat.Id,
                                            Config.GetResourceString("EditInputText"),
                                            cancellationToken: cancellationToken);
                return true;
            case "user_delete_group":
                await botClient.SendMessage(update.CallbackQuery.Message!.Chat.Id,
                                            Config.GetResourceString("DeleteInputText"),
                                            cancellationToken: cancellationToken);
                return true;
            default:
                if (action.StartsWith("user_change_group_name:"))
                {
                    groupId = int.Parse(action.Split(':')[1]);
                    await Utils.Utils.SendMessage(botClient,
                                                    update,
                                                    KeyboardUtils.GetReturnButtonMarkup(backCallback),
                                                    cancellationToken,
                                                    Config.GetResourceString("NewGroupNameText"));
                }
                else if (action.StartsWith("user_change_group_description:"))
                {
                    groupId = int.Parse(action.Split(':')[1]);
                    await Utils.Utils.SendMessage(botClient,
                                                    update,
                                                    KeyboardUtils.GetReturnButtonMarkup(backCallback),
                                                    cancellationToken,
                                                    Config.GetResourceString("NewGroupDescriptionText"));
                }
                else if (action.StartsWith("user_change_is_default:"))
                {
                    groupId = int.Parse(action.Split(':')[1]);
                    isDBActionSuccessful = DBforGroups.SetIsDefaultGroup(groupId);
                    groupInfo = string.Format(Config.GetResourceString("GroupInfoText"), DBforGroups.GetGroupNameById(groupId),
                                                DBforGroups.GetGroupDescriptionById(groupId), DBforGroups.GetGroupMemberCount(groupId), 
                                                DBforGroups.GetIsDefaultGroup(groupId));
                    await Utils.Utils.SendMessage(botClient, update, UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId), cancellationToken, groupInfo);
                    return false;
                }
                return true;
        }
    }

    public async Task<bool?> ProcessAction(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(action);
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        switch (action)
        {
            case "user_create_group":
                groupName = update.Message!.Text!;
                await Utils.Utils.SendMessage(
                    botClient,
                    update,
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup(),
                    cancellationToken,
                    Config.GetResourceString("ConfirmDecision"));
                return true;
            case "user_edit_group":
                if (int.TryParse(update.Message!.Text!, out groupId) && DBforGroups.CheckGroupOwnership(groupId, userId))
                {
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        UsersGroup.GetUsersGroupEditActionsKeyboardMarkup(groupId),
                        cancellationToken,
                        Config.GetResourceString("ChooseOptionText"));
                    return false;
                }
                return null;
            case "user_delete_group":
                if (int.TryParse(update.Message!.Text!, out groupId) && DBforGroups.CheckGroupOwnership(groupId, userId))
                {
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetConfirmForActionKeyboardMarkup(),
                        cancellationToken,
                        $"{groupInfo}\n{Config.GetResourceString("ConfirmDecision")}");
                    return true;
                }
                return null;
            default:
                if (action.StartsWith("user_change_group_name:"))
                {
                    groupId = int.Parse(action.Split(':')[1]);
                    groupName = update.Message!.Text!;
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetConfirmForActionKeyboardMarkup(denyCallback: backCallback),
                        cancellationToken,
                        Config.GetResourceString("ConfirmDecision"));
                    return true;
                }
                else if (action.StartsWith("user_change_group_description:"))
                {
                    groupId = int.Parse(action.Split(':')[1]);
                    description = update.Message!.Text!;
                    await Utils.Utils.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetConfirmForActionKeyboardMarkup(denyCallback: backCallback),
                        cancellationToken,
                        Config.GetResourceString("ConfirmDecision"));
                    return true;
                }
                return null;
        }
    }

    public void ProcessFinish(long chatId)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        switch (action)
        {
            case "user_create_group":
                isDBActionSuccessful = DBforGroups.AddGroup(userId, groupName, "");
                break;
            case "user_delete_group":
                isDBActionSuccessful = DBforGroups.DeleteGroup(groupId);
                break;
            default:
                if (action.StartsWith("user_change_group_name:"))
                {
                    isDBActionSuccessful = DBforGroups.SetGroupName(groupId, groupName);
                }
                else if (action.StartsWith("user_change_group_description:"))
                {
                    isDBActionSuccessful = DBforGroups.UpdateGroupDescription(groupId, description);
                }
                break;
        }
    }
}