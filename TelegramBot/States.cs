using DataBase;
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

    static string link;

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
                    await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    TelegramBot.userStates.Remove(chatId);
                    return;
                }
                link = update.Message.Text;
                if (DB.SearchContactByLink(link) == -1)
                {
                    await botClient.SendMessage(chatId, "По этой ссылке никто не найден.", cancellationToken: cancellationToken);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    TelegramBot.userStates.Remove(chatId);
                    return;
                }
                await botClient.SendMessage(chatId, "По этой ссылке найден один человек.", cancellationToken: cancellationToken, 
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                TelegramBot.userStates[chatId].State = ContactState.WaitingForName;
                break;

            case ContactState.WaitingForName:
                string text_data = $@"Ссылка: {link} 
Имя: {DB.GetUserNameByID(DB.SearchContactByLink(link))}";
                await botClient.SendMessage(chatId, "Подтвердите добавление (в противном случае напишите /start): " + text_data, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Дальше"));
                TelegramBot.userStates[chatId].State = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForConfirmation:
                if (update.Message.Text == "/start")
                {
                    await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    TelegramBot.userStates.Remove(chatId);
                    return;
                }
                DB.AddContact(chatId, link);
                await SendNotification(botClient, chatId, cancellationToken);
                await botClient.SendMessage(chatId, "Теперь ожидайте когда контакт также добавит вас в свой список.", 
                                            cancellationToken: cancellationToken, replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup("Ждёмс..."));
                TelegramBot.userStates[chatId].State = ContactState.FinishAddContact;
                break;

            case ContactState.FinishAddContact:
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                TelegramBot.userStates.Remove(chatId);
                break;
        }
    }

    public static async Task SendNotification(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(DB.GetTelegramIDbyUserID(DB.SearchContactByLink(link)), $"Пользователь {DB.GetUserNameByTelegramID(chatId)} хочет добавить вас в свои контакты.", cancellationToken: cancellationToken);
    }
}