// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.UnitOfWork.Services;

public class GroupUoWService : IGroupUoW
{
    private readonly IUnitOfWork _uow;
    private readonly IGroupRepository _repository;

    public GroupUoWService(IUnitOfWork uow, IGroupRepository repository)
    {
        _uow = uow;
        _repository = repository;
    }

    // Методы остаются теми же, но теперь они правильно реализуют IGroupUoW
    public async Task<bool> SetNewGroup(int userId, string groupName, string description)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.CreateGroup(userId, groupName, description));
        return affected > 0;
    }

    public async Task<bool> SetGroupName(int groupId, string groupName)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.UpdateGroupName(groupId, groupName));
        return affected > 0;
    }

    public async Task<bool> SetGroupDescription(int groupId, string description)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.UpdateGroupDescription(groupId, description));
        return affected > 0;
    }

    public async Task<bool> SetIsDefaultGroup(int groupId)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.ToggleDefaultStatus(groupId));
        return affected > 0;
    }

    public async Task<bool> SetDeleteGroup(int groupId)
    {
        int affected = await ExecuteInTransaction(() =>
            _repository.DeleteGroup(groupId));
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
