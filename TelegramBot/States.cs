using Telegram.Bot;
using Telegram.Bot.Types;


namespace MediaTelegramBot;


public enum ContactState
{
    WaitingForLink,
    WaitingForName,
    WaitingForConfirmation,
    FinishAddContact
}

public class ProcessContactState
{
    private static ContactState[] _allStates = (ContactState[])Enum.GetValues(typeof(ContactState));

    public static ContactState[] GetAllStates()
    {
        return _allStates;
    }
    public static async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = Utils.GetIDfromUpdate(update);
        if (Utils.CheckNonZeroID(chatId)) return;

        switch (TelegramBot.userStates[chatId].State)
        {
            case ContactState.WaitingForLink:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu")
                {
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    TelegramBot.userStates.Remove(chatId);
                    return;
                }
                await botClient.SendMessage(chatId, "По этой ссылке найден:", cancellationToken: cancellationToken);
                TelegramBot.userStates[chatId].State = ContactState.WaitingForName;
                break;

            case ContactState.WaitingForName:
                await botClient.SendMessage(chatId, "Подтвердите добавление:", cancellationToken: cancellationToken);
                TelegramBot.userStates[chatId].State = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForConfirmation:
                await botClient.SendMessage(chatId, "Ожидайте когда контакт также добавит вас в свой список.", cancellationToken: cancellationToken);
                TelegramBot.userStates[chatId].State = ContactState.FinishAddContact;
                break;

            case ContactState.FinishAddContact:
                await botClient.SendMessage(chatId, "Процесс завершен. Можете вернутся в меню.", cancellationToken: cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                TelegramBot.userStates.Remove(chatId);
                break;
        }
    }
}