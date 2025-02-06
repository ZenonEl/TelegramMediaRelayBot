// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

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

public enum UsersStandardState
{
    ProcessAction,
    ProcessData,
    Finish
}