// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.UnitOfWork.Services;

// 1. Добавляем ": IPrivacySettingsTargetsUoW"
public class PrivacySettingsTargetsUoWService : IPrivacySettingsTargetsUoW
{
    private readonly IUnitOfWork _uow;
    // 2. Меняем зависимость на IRepository
    private readonly IPrivacySettingsTargetsRepository _repository;

    public PrivacySettingsTargetsUoWService(IUnitOfWork uow, IPrivacySettingsTargetsRepository repository)
    {
        _uow = uow;
        _repository = repository;
    }

    // 3. Реализуем методы интерфейса, вызывая IRepository
    public async Task<bool> SetPrivacyRuleTarget(int userId, int privacySettingId, string targetType, string targetValue)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.UpsertTarget(userId, privacySettingId, targetType, targetValue));
        return affected > 0;
    }

    public async Task<bool> SetToRemovePrivacyRuleTarget(int privacySettingId, string targetValue)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.RemoveTarget(privacySettingId, targetValue));
        return affected > 0;
    }

    // Вспомогательный метод для транзакций
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
