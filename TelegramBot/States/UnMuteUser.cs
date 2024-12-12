using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace MediaTelegramBot;



public class ProcessUserUnMuteState : IUserState
{
    public UserUnMuteState currentState;

    private int mutedByUserId { get; set; }
    private int mutedContactId { get; set; }
    private DateTime? expirationDate { get; set; }

    public ProcessUserUnMuteState()
    {
        currentState = UserUnMuteState.WaitingForLinkOrID;
    }

    public static UserUnMuteState[] GetAllStates()
    {
        return (UserUnMuteState[])Enum.GetValues(typeof(UserUnMuteState));
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

        var userState = (ProcessUserUnMuteState)TelegramBot.userStates[chatId];

        switch (userState.currentState)
        {
            case UserUnMuteState.WaitingForLinkOrID:
                int contactId; 
                if (int.TryParse(update.Message.Text, out contactId))
                {
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));
                    string name = DBforGetters.GetUserNameByID(contactId);
                    
                    if (name == "" || !allowedIds.Contains(mutedContactId))
                    {
                        await Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "По этому ID никто не найден.");
                        return;
                    }
                    await botClient.SendMessage(chatId, $"Будем работать с этим контактом?\nID: {contactId} Имя: {name} ?", cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                }
                else
                {
                    string link = update.Message.Text;
                    contactId = DBforGetters.GetContactIDByLink(link);
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));

                    if (contactId == -1 || !allowedIds.Contains(mutedContactId))
                    {
                        await Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "По этой ссылке никто не найден.");
                        return;
                    }
                    string name = DBforGetters.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, $"Будем работать с этим контактом?\nID: {contactId}, Именем: {name} ?", cancellationToken: cancellationToken);
                }
                mutedByUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
                mutedContactId = contactId;
                await botClient.SendMessage(chatId, "Подтвердите решение (в противном случае напишите /start)", cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                userState.currentState = UserUnMuteState.WaitingForUnMute;
                break;

            case UserUnMuteState.WaitingForUnMute:
                if (await Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;

                string text = @"Пользователь находится в муте {время}\nВы действительно хотите его размутить? Если нет то напишите /start";
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Да"));
                userState.currentState = UserUnMuteState.Finish;
                break;

            case UserUnMuteState.Finish:
                if (await Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);

                CoreDB.UnMutedContact(mutedByUserId, mutedContactId);
                await Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "Пользователь размучен.");
                TelegramBot.userStates.Remove(chatId);
                break;
        }
    }

}
