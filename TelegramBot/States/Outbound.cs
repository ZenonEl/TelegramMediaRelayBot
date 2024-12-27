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
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.Contains("revoke_outbound_invite:"))
                {
                    await botClient.SendMessage(chatId, "Вы точно хотите отозвать приглашение? (в противном случае введите команду /start)", cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("YesButtonText")));
                    userState.currentState = UserOutboundState.Finish;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update);
                break;

            case UserOutboundState.Finish:
                if (update.Message != null && update.Message.Text == Config.GetResourceString("YesButtonText"))
                {
                    TelegramBot.userStates.Remove(chatId);
                    await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
        }
    }
}