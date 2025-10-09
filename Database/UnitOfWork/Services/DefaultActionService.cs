using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.UnitOfWork.Services;

public class DefaultActionUoWService : IDefaultActionUoW
{
    private readonly IUnitOfWork _uow;
    private readonly IDefaultActionRepository _repository;

    public DefaultActionUoWService(IUnitOfWork uow, IDefaultActionRepository repository)
    {
        _uow = uow;
        _repository = repository;
    }

    public async Task<bool> SetAutoSendVideoCondition(int userId, string actionCondition, string type)
    {
        var affectedRows = await ExecuteInTransaction(() => 
            _repository.UpsertActionCondition(userId, actionCondition, type));
        
        return affectedRows > 0;
    }

    public async Task<bool> SetAutoSendVideoAction(int userId, string action, string type)
    {
        var affectedRows = await ExecuteInTransaction(() => 
            _repository.UpsertAction(userId, action, type));
        
        return affectedRows > 0;
    }

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