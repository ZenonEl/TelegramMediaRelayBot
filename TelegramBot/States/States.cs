using DataBase;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace MediaTelegramBot;


public interface IUserState
{
    Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    string GetCurrentState();
}


public enum ContactState
{
    WaitingForLink,
    WaitingForName,
    WaitingForConfirmation,
    FinishAddContact
}

public enum UserMuteState
{
    WaitingForLinkOrID,
    WaitingForMuteTime,
    WaitingForConfirmation,
    Finish
}

public enum UserUnMuteState
{
    WaitingForLinkOrID,
    WaitingForUnMute,
    Finish
}

public enum UserOutboundState
{
    ProcessAction,
    Finish
}

public enum UserInboundState
{
    SelectInvite,
    ProcessAction,
    Finish
}

public enum UsersStandartState
{
    ProcessAction,
    ProcessData,
    Finish
}