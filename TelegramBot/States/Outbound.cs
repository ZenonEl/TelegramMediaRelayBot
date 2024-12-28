using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;


namespace MediaTelegramBot;

public class UserProcessOutboundState : IUserState
{
    public UserOutboundState currentState;

    public UserProcessOutboundState()
    {
        currentState = UserOutboundState.ProcessAction;
    }

    public static UserOutboundState[] GetAllStates()
    {
        return (UserOutboundState[])Enum.GetValues(typeof(UserOutboundState));
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

        var userState = (UserProcessOutboundState)value;

        switch (userState.currentState)
        {
            case UserOutboundState.ProcessAction:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("revoke_outbound_invite:"))
                {
                    string userId = update.CallbackQuery.Data.Split(':')[1];
                    await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetOutBoundActionsKeyboardMarkup(userId, "user_show_outbound_invite:" + chatId),
                                                cancellationToken, Config.GetResourceString("DeclineOutBound"));
                    userState.currentState = UserOutboundState.Finish;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update);
                break;

            case UserOutboundState.Finish:
                if (update.CallbackQuery != null)
                {
                    if (update.CallbackQuery.Data!.StartsWith("user_show_outbound_invite:"))
                    {
                        TelegramBot.userStates.Remove(chatId);
                        await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId);
                        return;
                    }
                    else if (update.CallbackQuery.Data!.StartsWith("user_accept_revoke_outbound_invite:"))
                    {
                        string userId = update.CallbackQuery.Data.Split(':')[1];
                        CoreDB.SetContactStatus(chatId, long.Parse(userId), DataBase.Types.ContactsStatus.DECLINED);
                    }
                }
                TelegramBot.userStates.Remove(chatId);
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
        }
    }
}