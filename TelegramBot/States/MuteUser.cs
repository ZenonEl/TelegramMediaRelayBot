using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TikTokMediaRelayBot;
using System.Globalization;

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
                if (int.TryParse(update.Message!.Text, out contactId))
                {
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));
                    string name = DBforGetters.GetUserNameByID(contactId);
                    if (name == "" || !allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    {
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("NoUserFoundByID", CultureInfo.CurrentUICulture)!);
                        return;
                    }
                    await botClient.SendMessage(chatId, string.Format(Config.resourceManager.GetString("WillWorkWithContact", CultureInfo.CurrentUICulture)!, contactId, name), cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("NextButtonText", CultureInfo.CurrentUICulture)!));
                }
                else
                {
                    string link = update.Message.Text!;
                    contactId = DBforGetters.GetContactIDByLink(link);
                    List<long> allowedIds = await CoreDB.GetContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));

                    if (contactId == -1 || !allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    {
                        await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("NoUserFoundByLink", CultureInfo.CurrentUICulture)!);
                        return;
                    }
                    string name = DBforGetters.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, string.Format(Config.resourceManager.GetString("WillWorkWithContact", CultureInfo.CurrentUICulture)!, contactId, name), cancellationToken: cancellationToken);
                }
                mutedByUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
                mutedContactId = contactId;
                await botClient.SendMessage(chatId, Config.resourceManager.GetString("ConfirmDecision", CultureInfo.CurrentUICulture)!, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("NextButtonText", CultureInfo.CurrentUICulture)!));
                userState.currentState = UserMuteState.WaitingForConfirmation;
                break;

            case UserMuteState.WaitingForConfirmation:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;

                string text = Config.resourceManager.GetString("MuteTimeInstructions", CultureInfo.CurrentUICulture)!;
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("IndefinitelyButtonText", CultureInfo.CurrentUICulture)!));
                userState.currentState = UserMuteState.WaitingForMuteTime;
                break;

            case UserMuteState.WaitingForMuteTime:
                string muteTime = update.Message!.Text!;

                if (int.TryParse(muteTime, out int time))
                {
                    DateTime unmuteTime = DateTime.Now.AddSeconds(time);
                    expirationDate = unmuteTime;
                    string unmuteMessage = string.Format(Config.resourceManager.GetString("UserWillBeUnmuted", CultureInfo.CurrentUICulture)!, unmuteTime, time);
                    await botClient.SendMessage(chatId, unmuteMessage, cancellationToken: cancellationToken);
                }
                else if (DateTime.TryParse(muteTime, out DateTime specifiedDate))
                {
                    expirationDate = specifiedDate;
                    await botClient.SendMessage(chatId, string.Format(Config.resourceManager.GetString("UserWillBeUnmutedAt", CultureInfo.CurrentUICulture)!, specifiedDate), cancellationToken: cancellationToken);
                }
                else if (muteTime.Equals(Config.resourceManager.GetString("IndefinitelyButtonText", CultureInfo.CurrentUICulture)!, StringComparison.OrdinalIgnoreCase))
                {
                    expirationDate = null;
                    await botClient.SendMessage(chatId, Config.resourceManager.GetString("UserWillBeMutedIndefinitely", CultureInfo.CurrentUICulture)!, cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(chatId, Config.resourceManager.GetString("InvalidMuteTimeFormat", CultureInfo.CurrentUICulture)!, cancellationToken: cancellationToken);
                    return;
                }
                await botClient.SendMessage(chatId, Config.resourceManager.GetString("ConfirmFinalDecision", CultureInfo.CurrentUICulture)!, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.resourceManager.GetString("NextButtonText", CultureInfo.CurrentUICulture)!));
                userState.currentState = UserMuteState.Finish;
                break;

            case UserMuteState.Finish:
                if (await Utils.Utils.HandleStateBreakCommand(botClient, update, cancellationToken, chatId)) return;
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);

                TelegramBot.userStates.Remove(chatId);
                if (!CoreDB.AddMutedContact(mutedByUserId, mutedContactId, expirationDate))
                {
                    await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("ActionCancelledError", CultureInfo.CurrentUICulture)!);
                    return;
                }
                await Utils.Utils.AlertMessageAndShowMenu(botClient, update, cancellationToken, chatId, Config.resourceManager.GetString("MuteSet", CultureInfo.CurrentUICulture)!);
                break;
        }
    }
}