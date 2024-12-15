using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;


namespace MediaTelegramBot;



public class ProcessUserMuteState : IUserState
{
    public UserMuteState currentState;

    private int mutedByUserId { get; set; }
    private int mutedContactId { get; set; }
    private DateTime? expirationDate { get; set; }

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
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

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
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));
                    string name = DBforGetters.GetUserNameByID(contactId);

                    if (name == "" || !allowedIds.Contains(mutedContactId))
                    {
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "По этому ID никто не найден.");
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
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "По этой ссылке никто не найден.");
                        return;
                    }
                    string name = DBforGetters.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, $"Будем работать с этим контактом?\nID: {contactId}, Именем: {name} ?", cancellationToken: cancellationToken);
                }
                mutedByUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
                mutedContactId = contactId;
                await botClient.SendMessage(chatId, "Подтвердите решение (в противном случае напишите /start)", cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                userState.currentState = UserMuteState.WaitingForConfirmation;
                break;

            case UserMuteState.WaitingForConfirmation:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;

                string text = @"
Чтобы перевести секунды в другие единицы времени, используйте следующие соотношения:

    1 минута = 60 секунд
    1 час = 60 минут = 3600 секунд
    1 день = 24 часа = 1440 минут = 86400 секунд

Таким образом, для перевода:

    Чтобы перевести секунды в минуты, разделите количество секунд на 60.
    Чтобы перевести секунды в часы, разделите количество секунд на 3600.
    Чтобы перевести секунды в дни, разделите количество секунд на 86400.
    
Последний шаг и мы его замутим. Теперь введите время мута в секундах:";
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Бессрочно"));
                userState.currentState = UserMuteState.WaitingForMuteTime;
                break;

            case UserMuteState.WaitingForMuteTime:
                string muteTime = update.Message.Text;

                if (int.TryParse(muteTime, out int time))
                {
                    DateTime unmuteTime = DateTime.Now.AddSeconds(time);
                    expirationDate = unmuteTime;
                    string unmuteMessage = $"Пользователь будет размучен в {unmuteTime:yyyy-MM-dd HH:mm:ss} (через {time} секунд).";
                    await botClient.SendMessage(chatId, unmuteMessage, cancellationToken: cancellationToken);
                }
                else if (DateTime.TryParse(muteTime, out DateTime specifiedDate))
                {
                    expirationDate = specifiedDate;
                    await botClient.SendMessage(chatId, $"Пользователь будет размучен в {specifiedDate:yyyy-MM-dd HH:mm:ss}.", cancellationToken: cancellationToken);
                }
                else if (muteTime.Equals("Бессрочно", StringComparison.OrdinalIgnoreCase))
                {
                    expirationDate = null;
                    await botClient.SendMessage(chatId, "Пользователь будет замучен бессрочно.", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(chatId, "Пожалуйста, введите время мута в секундах или дату в формате ГГГГ-ММ-ДД ЧЧ:ММ:СС. Или напишите 'Бессрочно'.", cancellationToken: cancellationToken);
                    return;
                }
                await botClient.SendMessage(chatId, "Проверьте своё решение и подтвердите его если вы уверены что это то что вам нужно (в противном случае напишите /start)", cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                userState.currentState = UserMuteState.Finish;
                break;

            case UserMuteState.Finish:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);

                TelegramBot.userStates.Remove(chatId);
                if (!CoreDB.AddMutedContact(mutedByUserId, mutedContactId, expirationDate))
                {
                    await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "Произошла ошибка. Действия отменены.");
                    return;
                }
                await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "Мут установлен.");
                break;
        }
    }
}
