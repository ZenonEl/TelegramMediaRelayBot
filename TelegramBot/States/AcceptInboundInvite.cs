using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Utils;
using TikTokMediaRelayBot;


namespace MediaTelegramBot;

public class UserProcessInboundState : IUserState
{
    public UserInboundState currentState;

    public UserProcessInboundState()
    {
        currentState = UserInboundState.SelectInvite;
    }

    public static UserInboundState[] GetAllStates()
    {
        return (UserInboundState[])Enum.GetValues(typeof(UserInboundState));
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

        var userState = (UserProcessInboundState)value;

        switch (userState.currentState)
        {
            case UserInboundState.SelectInvite:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("user_show_inbounds_invite:"))
                {
                    string userId = update.CallbackQuery.Data.Split(':')[1];
                    await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetInBoundActionsKeyboardMarkup(userId, "view_inbound_invite_links"),
                                                cancellationToken, "Выберите действие с запросом:");
                    userState.currentState = UserInboundState.ProcessAction;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;

            case UserInboundState.ProcessAction:
                if (update.Message != null && update.Message.Text != null)
                {
                    TelegramBot.userStates.Remove(chatId);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    return;
                }
                else if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("view_inbound_invite_links"))
                {
                    await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, chatId);
                    return;
                }
                string userID = update.CallbackQuery!.Data!.Split(':')[1];
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("user_accept_inbounds_invite:"))
                {
                    await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetConfirmForActionKeyboardMarkup($"accept_accept_invite:{userID}", $"decline_accept_invite:{userID}"),
                    cancellationToken, "Вы точно хотите принять запрос? (в противном случае введите команду /start)");
                }
                else if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("user_decline_inbounds_invite:"))
                {
                    await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetConfirmForActionKeyboardMarkup($"accept_decline_invite:{userID}", $"decline_decline_invite:{userID}"), 
                    cancellationToken, "Вы точно хотите отменить запрос? (в противном случае введите команду /start)");
                }

                userState.currentState = UserInboundState.Finish;
                break;

            case UserInboundState.Finish:
                if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("accept_accept_invite:"))
                {
                    await CallbackQueryMenuUtils.AcceptInboundInvite(update);
                }
                else if (update.CallbackQuery != null && update.CallbackQuery.Data!.StartsWith("accept_decline_invite:"))
                {
                    await CallbackQueryMenuUtils.DeclineInboundInvite(update);
                }
                else if (update.CallbackQuery != null && !update.CallbackQuery.Data!.StartsWith("main_menu"))
                {
                    string userId = update.CallbackQuery.Data.Split(':')[1];
                    await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetInBoundActionsKeyboardMarkup(userId, "view_inbound_invite_links"),
                                                cancellationToken, "Выберите действие с запросом:");
                    userState.currentState = UserInboundState.ProcessAction;
                    return;
                }
                TelegramBot.userStates.Remove(chatId);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
        }
    }
}