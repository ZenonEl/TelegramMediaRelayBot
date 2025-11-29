// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.




namespace TelegramMediaRelayBot;


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

public enum UsersStandardState
{
    ProcessAction,
    ProcessData,
    Finish
}