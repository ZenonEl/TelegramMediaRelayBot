using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace MediaTelegramBot;



public class ProcessContactState : IUserState
{
    private string link;
    public ContactState currentState;

    public ProcessContactState()
    {
        currentState = ContactState.WaitingForLink;
    }

    public static ContactState[] GetAllStates()
    {
        return (ContactState[])Enum.GetValues(typeof(ContactState));
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.GetIDfromUpdate(update);
        if (Utils.CheckNonZeroID(chatId)) return;

        switch (currentState)
        {
            case ContactState.WaitingForLink:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu")
                {
                    await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    TelegramBot.userStates.Remove(chatId);
                    return;
                }
                
                link = update.Message.Text;
                
                if (DBforGetters.GetContactIDByLink(link) == -1)
                {
                    await Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, "По этой ссылке никто не найден.");
                    return;
                }
                
                await botClient.SendMessage(chatId, "По этой ссылке найден один человек.", cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                
                currentState = ContactState.WaitingForName;
                break;

            case ContactState.WaitingForName:
                string text_data = $"Ссылка: {link} \nИмя: {DBforGetters.GetUserNameByID(DBforGetters.GetContactIDByLink(link))}";
                
                await botClient.SendMessage(chatId, "Подтвердите добавление (в противном случае напишите /start): " + text_data, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                
                currentState = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForConfirmation:
                if (await Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;
                
                CoreDB.AddContact(chatId, link);
                
                await SendNotification(botClient, chatId, cancellationToken);
                await botClient.SendMessage(chatId, "Теперь ожидайте когда контакт также добавит вас в свой список.", 
                                            cancellationToken: cancellationToken, replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Ждёмс..."));
                
                currentState = ContactState.FinishAddContact;
                
                break;

            case ContactState.FinishAddContact:
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                
                TelegramBot.userStates.Remove(chatId);
                break;
        }
    }

    public async Task SendNotification(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(DBforGetters.GetTelegramIDbyUserID(DBforGetters.GetContactIDByLink(link)), $"Пользователь {DBforGetters.GetUserNameByTelegramID(chatId)} хочет добавить вас в свои контакты.", cancellationToken: cancellationToken);
    }
}
