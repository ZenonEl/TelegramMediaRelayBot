using Telegram.Bot;
using Telegram.Bot.Types;
using DataBase;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;


namespace MediaTelegramBot;

public class ProcessVideoDC : IUserState
{
    public UsersGroupState currentState;
    public string link { get; set; }
    public Message statusMessage { get; set; }
    public string text { get; set; }
    private string action = "";
    private List<long> targetUserIds = new List<long>();

    public ProcessVideoDC(string Link, Message StatusMessage, string Text)
    {
        link = Link;
        statusMessage = StatusMessage;
        text = Text;
        currentState = UsersGroupState.ProcessAction;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.Utils.GetIDfromUpdate(update);

        switch (currentState)
        {
            case UsersGroupState.ProcessAction:
                if (update.CallbackQuery != null)
                {
                    string callbackData = update.CallbackQuery.Data!;
                    switch (callbackData)
                    {
                        case "send_to_all_contacts":
                            action = "send_to_all_contacts";
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;
                        case "send_to_default_groups":
                            action = "send_to_default_groups";
                            await PrepareTargetUserIds(chatId);
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            break;
                        case "send_to_specified_groups":
                            action = "send_to_specified_groups";
                            await botClient.SendMessage(chatId, "Please enter group IDs separated by spaces:", cancellationToken: cancellationToken);
                            currentState = UsersGroupState.ProcessData;
                            break;
                        case "send_to_specified_users":
                            action = "send_to_specified_users";
                            await botClient.SendMessage(chatId, "Please enter user IDs separated by spaces:", cancellationToken: cancellationToken);
                            currentState = UsersGroupState.ProcessData;
                            break;
                        case "send_only_to_me":
                            action = "send_only_to_me";
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("ConfirmDecision"), replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                            currentState = UsersGroupState.Finish;
                            break;
                        case "main_menu":
                            await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false);
                            break;
                    }
                }
                break;

            case UsersGroupState.ProcessData:
                if (update.Message != null)
                {
                    string input = update.Message.Text!;
                    if (input.Contains(" "))
                    {
                        string[] ids = input.Split(' ');
                        if (ids.All(id => long.TryParse(id, out _)))
                        {
                            targetUserIds = ids.Select(long.Parse).ToList();
                            await PrepareTargetUserIds(chatId);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, "Invalid input. Please enter numbers separated by spaces.", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        if (long.TryParse(input, out long id))
                        {
                            targetUserIds.Add(id);
                            await PrepareTargetUserIds(chatId);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, "Invalid input. Please enter a number.", cancellationToken: cancellationToken);
                        }
                    }
                }
                break;

            case UsersGroupState.Finish:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu" ||
                    update.Message != null)
                {
                    await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("VideoDistributionQuestion"), replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(), cancellationToken: cancellationToken);
                    currentState = UsersGroupState.ProcessAction;
                    return;
                }
                await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, Config.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);
                TelegramBot.userStates.Remove(chatId);
                _ = TelegramBot.HandleVideoRequest(botClient, link, chatId, statusMessage, targetUserIds, caption: text);
                break;
        }
    }

    private async Task PrepareTargetUserIds(long chatId)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = DBforGetters.GetUsersIdForMuteContactId(userId);

        switch (action)
        {
            case "send_to_all_contacts":
                List<long> contactUserTGIds = await CoreDB.GetAllContactUserTGIds(userId);
                targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                break;

            case "send_to_default_groups":
                List<int> defaultGroupContactIDs = DBforGroups.GetAllUsersInDefaultEnabledGroups(userId);

                targetUserIds = defaultGroupContactIDs
                    .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    .Select(DBforGetters.GetTelegramIDbyUserID)
                    .ToList();
                break;
            case "send_to_specified_groups":
            case "send_to_specified_users":
                break;
        }

        currentState = UsersGroupState.Finish;
    }
}