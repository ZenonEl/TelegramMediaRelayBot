using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TikTokMediaRelayBot;


namespace MediaTelegramBot;

public class ProcessUserProcessOutboundState : IUserState
{
    public UserProcessOutboundState currentState;

    public ProcessUserProcessOutboundState()
    {
        currentState = UserProcessOutboundState.ProcessAction;
    }

    public static UserProcessOutboundState[] GetAllStates()
    {
        return (UserProcessOutboundState[])Enum.GetValues(typeof(UserProcessOutboundState));
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

        var userState = (ProcessUserProcessOutboundState)value;

        switch (userState.currentState)
        {
            case UserProcessOutboundState.ProcessAction:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.Contains("revoke_outbound_invite:"))
                {
                    await botClient.SendMessage(chatId, "Вы точно хотите отозвать приглашение? (в противном случае введите команду /start)", cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("YesButtonText")));
                    userState.currentState = UserProcessOutboundState.Finish;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update);
                break;

            case UserProcessOutboundState.Finish:
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