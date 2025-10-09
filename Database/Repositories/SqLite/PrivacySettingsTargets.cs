// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Dapper;
using System.Data;
using TelegramMediaRelayBot.Database.UnitOfWork.Services;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqlitePrivacySettingsTargetsSetter(PrivacySettingsTargetsUoWService uowService) : IPrivacySettingsTargetsSetter
{
    public Task<bool> SetPrivacyRuleTarget(int userId, int privacySettingId, string targetType, string targetValue)
    {
        return uowService.SetPrivacyRuleTarget(userId, privacySettingId, targetType, targetValue);
    }


    public Task<bool> SetToRemovePrivacyRuleTarget(int privacySettingId, string targetValue)
    {
        return uowService.SetToRemovePrivacyRuleTarget(privacySettingId, targetValue);
    }
}

public class SqlitePrivacySettingsTargetsGetter(IPrivacySettingsTargetsRepository repository) : IPrivacySettingsTargetsGetter
{
    public Task<bool> CheckPrivacyRuleTargetExists(int userId, string type)
    {
        return repository.CheckTargetExists(userId, type);
    }

    public Task<List<string>> GetAllActiveUserRuleTargets(int userId)
    {
        return repository.GetAllUserTargets(userId);
    }
}