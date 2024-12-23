using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TikTokMediaRelayBot;


namespace MediaTelegramBot;

public class ProcessUserUnMuteState : IUserState
{
    public UserUnMuteState currentState;

    private int mutedByUserId { get; set; }
    private int mutedContactId { get; set; }

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
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

        if (!TelegramBot.userStates.TryGetValue(chatId, out IUserState? value))
        {
            return;
        }

        var userState = (ProcessUserUnMuteState)value;

        switch (userState.currentState)
        {
            case UserUnMuteState.WaitingForLinkOrID:
                int contactId;
                if (int.TryParse(update.Message!.Text, out contactId))
                {
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));
                    string name = DBforGetters.GetUserNameByID(contactId);

                    if (name == "" || !allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    {
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByID"));
                        return;
                    }
                    await botClient.SendMessage(chatId, string.Format(Config.GetResourceString("WillWorkWithContact"), contactId, name), cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));
                }
                else
                {
                    string link = update.Message.Text!;
                    contactId = DBforGetters.GetContactIDByLink(link);
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));

                    if (contactId == -1 || !allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    {
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByLink"));
                        return;
                    }
                    string name = DBforGetters.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, string.Format(Config.GetResourceString("WillWorkWithContact"), contactId, name), cancellationToken: cancellationToken);
                }
                mutedByUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
                mutedContactId = contactId;
                await botClient.SendMessage(chatId, Config.GetResourceString("ConfirmDecision"), cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));
                userState.currentState = UserUnMuteState.WaitingForUnMute;
                break;

            case UserUnMuteState.WaitingForUnMute:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId)) return;

                string activeMuteTime = DBforGetters.GetActiveMuteTimeByContactID(mutedContactId);
                string text = string.Format(Config.GetResourceString("UserInMute"), activeMuteTime);
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("YesButtonText")));
                userState.currentState = UserUnMuteState.Finish;
                break;

            case UserUnMuteState.Finish:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, chatId)) return;
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);

                CoreDB.UnMutedContact(mutedByUserId, mutedContactId);
                await Utils.Utils.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("UserUnmuted"));
                TelegramBot.userStates.Remove(chatId);
                break;
        }
    }
}