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
        long chatId = Utils.Utils.GetIDfromUpdate(update);
        if (Utils.Utils.CheckNonZeroID(chatId)) return;

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
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("NoUserFoundByID", System.Globalization.CultureInfo.CurrentUICulture));
                        return;
                    }
                    await botClient.SendMessage(chatId, string.Format(Config.resourceManager.GetString("WillWorkWithContact", System.Globalization.CultureInfo.CurrentUICulture), contactId, name), cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("NextButtonText", System.Globalization.CultureInfo.CurrentUICulture)));
                }
                else
                {
                    string link = update.Message.Text;
                    contactId = DBforGetters.GetContactIDByLink(link);
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));

                    if (contactId == -1 || !allowedIds.Contains(mutedContactId))
                    {
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("NoUserFoundByLink", System.Globalization.CultureInfo.CurrentUICulture));
                        return;
                    }
                    string name = DBforGetters.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, string.Format(Config.resourceManager.GetString("WillWorkWithContact", System.Globalization.CultureInfo.CurrentUICulture), contactId, name), cancellationToken: cancellationToken);
                }
                mutedByUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
                mutedContactId = contactId;
                await botClient.SendMessage(chatId, Config.resourceManager.GetString("ConfirmDecision", System.Globalization.CultureInfo.CurrentUICulture), cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("NextButtonText", System.Globalization.CultureInfo.CurrentUICulture)));
                userState.currentState = UserUnMuteState.WaitingForUnMute;
                break;

            case UserUnMuteState.WaitingForUnMute:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;

                string text = string.Format(Config.resourceManager.GetString("UserInMute", System.Globalization.CultureInfo.CurrentUICulture), Config.resourceManager.GetString("TimePlaceholder", System.Globalization.CultureInfo.CurrentUICulture));
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("YesButtonText", System.Globalization.CultureInfo.CurrentUICulture)));
                userState.currentState = UserUnMuteState.Finish;
                break;

            case UserUnMuteState.Finish:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);

                CoreDB.UnMutedContact(mutedByUserId, mutedContactId);
                await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("UserUnmuted", System.Globalization.CultureInfo.CurrentUICulture));
                TelegramBot.userStates.Remove(chatId);
                break;
        }
    }
}