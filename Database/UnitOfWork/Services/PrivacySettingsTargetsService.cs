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
        var affected = await ExecuteInTransaction(() => 
            _repository.UpsertTarget(userId, privacySettingId, targetType, targetValue));
        return affected > 0;
    }

    public async Task<bool> SetToRemovePrivacyRuleTarget(int privacySettingId, string targetValue)
    {
        var affected = await ExecuteInTransaction(() => 
            _repository.RemoveTarget(privacySettingId, targetValue));
        return affected > 0;
    }
    
    // Вспомогательный метод для транзакций
    private async Task<T> ExecuteInTransaction<T>(Func<Task<T>> action)
    {
        try
        {
            _uow.Begin();
            var result = await action();
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