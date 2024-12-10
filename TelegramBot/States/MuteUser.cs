using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace MediaTelegramBot;



public class ProcessUserMuteState : IUserState
{
    public UserMuteState currentState;

    public ProcessUserMuteState()
    {
        currentState = UserMuteState.WaitingForLinkOrID;
    }

    public static UserMuteState[] GetAllStates()
    {
        return (UserMuteState[])Enum.GetValues(typeof(UserMuteState));
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.GetIDfromUpdate(update);
        if (Utils.CheckNonZeroID(chatId)) return;

        if (!TelegramBot.userStates.ContainsKey(chatId))
        {
            return;
        }

        var userState = (ProcessUserMuteState)TelegramBot.userStates[chatId];

        switch (userState.currentState)
        {
            case UserMuteState.WaitingForLinkOrID:
                int contactId; 
                if (int.TryParse(update.Message.Text, out contactId))
                {
                    string name = DBforGetters.GetUserNameByID(contactId);
                    if (name == "")
                    {
                        await Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "По этому ID никто не найден.");
                        return;
                    }
                    await botClient.SendMessage(chatId, $"Вы точно хотите замутить человека с ID: {contactId} {name} ?", cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                }
                else
                {
                    string link = update.Message.Text;
                    contactId = DBforGetters.GetContactIDByLink(link);
                    if (contactId == -1)
                    {
                        await Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "По этой ссылке никто не найден.");
                        return;
                    }
                    string name = DBforGetters.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, $"Вы точно хотите замутить человека с ID: {contactId}, Именем: {name} ?", cancellationToken: cancellationToken);
                }
                await botClient.SendMessage(chatId, "Подтвердите мут (в противном случае напишите /start)", cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                userState.currentState = UserMuteState.WaitingForConfirmation;
                break;

            case UserMuteState.WaitingForConfirmation:
                await Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId);
                userState.currentState = UserMuteState.Finish;
                break;

            case UserMuteState.Finish:
                await botClient.SendMessage(chatId, "Процесс завершен.", cancellationToken: cancellationToken);
                TelegramBot.userStates.Remove(chatId);
                break;

            default:
                await botClient.SendMessage(chatId, "Неизвестное состояние.", cancellationToken: cancellationToken);
                break;
        }
    }

}
