using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.UnitOfWork.Services;

public class GroupUoWService : IGroupSetter
{
    private readonly IUnitOfWork _uow;
    private readonly IGroupRepository _repository;

    public GroupUoWService(IUnitOfWork uow, IGroupRepository repository)
    {
        _uow = uow;
        _repository = repository;
    }

    public async Task<bool> SetNewGroup(int userId, string groupName, string description)
    {
        var affected = await ExecuteInTransaction(() => 
            _repository.CreateGroup(userId, groupName, description));
        return affected > 0;
    }

    public async Task<bool> SetGroupName(int groupId, string groupName)
    {
        var affected = await ExecuteInTransaction(() => 
            _repository.UpdateGroupName(groupId, groupName));
        return affected > 0;
    }

    public async Task<bool> SetGroupDescription(int groupId, string description)
    {
        var affected = await ExecuteInTransaction(() => 
            _repository.UpdateGroupDescription(groupId, description));
        return affected > 0;
    }

    public async Task<bool> SetIsDefaultGroup(int groupId)
    {
        var affected = await ExecuteInTransaction(() => 
            _repository.ToggleDefaultStatus(groupId));
        return affected > 0;
    }

    public async Task<bool> SetDeleteGroup(int groupId)
    {
        var affected = await ExecuteInTransaction(() => 
            _repository.DeleteGroup(groupId));
        return affected > 0;
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