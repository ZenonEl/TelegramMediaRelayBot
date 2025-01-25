// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


namespace DataBase.Types;

public class ButtonData
{
    public required string ButtonText { get; set; }
    public required string CallbackData { get; set; }
}

public class ContactsStatus
{
    public const string WAITING_FOR_ACCEPT = "waiting_for_accept";
    public const string ACCEPTED = "accepted";
    public const string DECLINED = "declined";
}