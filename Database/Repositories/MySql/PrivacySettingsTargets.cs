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
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database.UnitOfWork.Services;


namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlPrivacySettingsTargetsSetter(IPrivacySettingsTargetsUoW uowService) : IPrivacySettingsTargetsSetter
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

public class MySqlPrivacySettingsTargetsGetter(IDbConnection dbConnection) : IPrivacySettingsTargetsGetter
{
    public async Task<bool> CheckPrivacyRuleTargetExists(int userId, string type)
    {
        const string query = @"
            SELECT EXISTS(
                SELECT 1 FROM PrivacySettingsTargets
                WHERE UserId = @userId AND TargetType = @type
                LIMIT 1
            )";


        var result = await dbConnection.ExecuteScalarAsync<bool>(query, new { userId, type });
        return result;
    }

    public async Task<List<string>> GetAllActiveUserRuleTargets(int userId)
    {
        const string query = @"
            SELECT TargetValue
            FROM PrivacySettingsTargets
            WHERE UserId = @userId";


        return (List<string>)await dbConnection.QueryAsync<List<string>>(query, new { userId });
    }
}
