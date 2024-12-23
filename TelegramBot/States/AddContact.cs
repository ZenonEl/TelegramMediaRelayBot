using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TikTokMediaRelayBot;


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
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

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

                link = update.Message!.Text!;

                if (DBforGetters.GetContactIDByLink(link) == -1)
                {
                    await Utils.Utils.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByLink"));
                    return;
                }

                await botClient.SendMessage(chatId, Config.GetResourceString("UserFoundByLink"), cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));

                currentState = ContactState.WaitingForName;
                break;

            case ContactState.WaitingForName:
                string text_data = $"{Config.GetResourceString("LinkText")}: {link} \n{Config.GetResourceString("NameText")}: {DBforGetters.GetUserNameByID(DBforGetters.GetContactIDByLink(link))}";

                await botClient.SendMessage(chatId, Config.GetResourceString("ConfirmAdditionText") + text_data, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));

                currentState = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForConfirmation:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId)) return;

                CoreDB.AddContact(chatId, link);

                await SendNotification(botClient, chatId, cancellationToken);
                await botClient.SendMessage(chatId, Config.GetResourceString("WaitForContactConfirmation"),
                                            cancellationToken: cancellationToken, replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("WaitForButtonText")));

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
        await botClient.SendMessage(DBforGetters.GetTelegramIDbyUserID(DBforGetters.GetContactIDByLink(link)), 
                                    string.Format(Config.GetResourceString("UserWantsToAddYou"), DBforGetters.GetUserNameByTelegramID(chatId)), 
                                    cancellationToken: cancellationToken);
    }
}