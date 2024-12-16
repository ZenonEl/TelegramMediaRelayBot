using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TikTokMediaRelayBot;
using System.Globalization;

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
                    await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("NoUserFoundByLink", CultureInfo.CurrentUICulture)!);
                    return;
                }

                await botClient.SendMessage(chatId, Config.resourceManager.GetString("UserFoundByLink", CultureInfo.CurrentUICulture)!, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("NextButtonText", CultureInfo.CurrentUICulture)!));

                currentState = ContactState.WaitingForName;
                break;

            case ContactState.WaitingForName:
                string text_data = $"{Config.resourceManager.GetString("LinkText", CultureInfo.CurrentUICulture)}: {link} \n{Config.resourceManager.GetString("NameText", CultureInfo.CurrentUICulture)}: {DBforGetters.GetUserNameByID(DBforGetters.GetContactIDByLink(link))}";

                await botClient.SendMessage(chatId, Config.resourceManager.GetString("ConfirmAdditionText", CultureInfo.CurrentUICulture) + text_data, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("NextButtonText", CultureInfo.CurrentUICulture)!));

                currentState = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForConfirmation:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;

                CoreDB.AddContact(chatId, link);

                await SendNotification(botClient, chatId, cancellationToken);
                await botClient.SendMessage(chatId, Config.resourceManager.GetString("WaitForContactConfirmation", CultureInfo.CurrentUICulture)!,
                                            cancellationToken: cancellationToken, replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("WaitForButtonText", CultureInfo.CurrentUICulture)!));

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
                                    string.Format(Config.resourceManager.GetString("UserWantsToAddYou", CultureInfo.CurrentUICulture)!, DBforGetters.GetUserNameByTelegramID(chatId)), 
                                    cancellationToken: cancellationToken);
    }
}