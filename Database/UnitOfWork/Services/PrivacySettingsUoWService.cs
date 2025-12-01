// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.UnitOfWork.Services;

public class PrivacySettingsUoWService : IPrivacySettingsUoW
{
    private readonly IUnitOfWork _uow;
    private readonly IPrivacySettingsRepository _repository;

    public PrivacySettingsUoWService(IUnitOfWork uow, IPrivacySettingsRepository repository)
    {
        _uow = uow;
        _repository = repository;
    }

    public async Task<bool> SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.UpsertRule(userId, type, action, isActive, actionCondition));
        return affected > 0;
    }

    public async Task<bool> SetPrivacyRuleToDisabled(int userId, string type)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.DisableRule(userId, type));
        return affected > 0;
    }

    private async Task<T> ExecuteInTransaction<T>(Func<Task<T>> action)
    {
        try
        {
            _uow.Begin();
            T? result = await action();
            _uow.Commit();
            return result;
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }
}
