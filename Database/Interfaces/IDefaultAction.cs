// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IDefaultAction
{
    public bool AddDefaultUsersActionTargets(int userId, int actionId, string targetType, int targetId);

    public bool RemoveAllDefaultUsersActionTargets(int userId, string targetType, int actionId);
}

public interface IDefaultActionSetter
{
    public bool SetAutoSendVideoConditionToUser(int userId, string actionCondition, string type);

    public bool SetAutoSendVideoActionToUser(int userId, string action, string type);

}

public interface IDefaultActionGetter
{
    public List<int> GetAllDefaultUsersActionTargets(int userId, string targetType, int actionId);

    public int GetDefaultActionId(int userId, string type);
    public string GetDefaultActionByUserIDAndType(int userID, string type);
}