using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.Database.UnitOfWork.Services;

public class ContactUoWService : IContactUoW
{
    private readonly IUnitOfWork _uow;
    private readonly IContactRepository _repository;
    private readonly IUserGetter _userGetter;
    private readonly IContactGetter _contactGetter;

    public ContactUoWService(IUnitOfWork uow, IContactRepository repository, IUserGetter userGetter, IContactGetter contactGetter)
    {
        _uow = uow;
        _repository = repository;
        _userGetter = userGetter;
        _contactGetter = contactGetter;
    }

    public async Task AddContactAsync(long userTelegramId, string contactLink, string status)
    {
        // Здесь мы правильно используем DI для получения ID
        var userId = _userGetter.GetUserIDbyTelegramID(userTelegramId);
        var contactId = _contactGetter.GetContactIDByLink(contactLink);
        
        if (userId == 0 || contactId == 0) return; // или бросить исключение

        await ExecuteInTransactionAsync(() => 
            _repository.AddContactAsync(userId, contactId, status));
    }

    public Task MuteContactAsync(int mutedByUserId, int mutedContactId, DateTime? expirationDate)
    {
        return ExecuteInTransactionAsync(() => 
            _repository.UpsertMutedContactAsync(mutedByUserId, mutedContactId, DateTime.UtcNow, expirationDate));
    }

    public Task UnmuteContactAsync(int userId, int contactId)
    {
        return ExecuteInTransactionAsync(() => 
            _repository.DeactivateMutedContactAsync(userId, contactId));
    }

    public Task UnMuteUserByMuteId(int muteId)
    {
        return ExecuteInTransactionAsync(() => 
            _repository.UnMuteUserByMuteId(muteId));
    }

    public Task RemoveContactByStatusAsync(int senderTelegramId, int accepterTelegramId, string? status = null)
    {
        // Здесь можно добавить логику получения ID по TelegramID, если нужно
        return ExecuteInTransactionAsync(() =>
            _repository.RemoveContactByStatusAsync(senderTelegramId, accepterTelegramId, status));
    }
    
    public async Task UpdateContactStatusAsync(long senderTelegramId, long accepterTelegramId, string status)
    {
        var senderId = _userGetter.GetUserIDbyTelegramID(senderTelegramId);
        var accepterId = _userGetter.GetUserIDbyTelegramID(accepterTelegramId);

        if (senderId == 0 || accepterId == 0) return;

        await ExecuteInTransactionAsync(() => 
            _repository.UpdateContactStatusAsync(senderId, accepterId, status));
    }
    
    public Task RemoveUsersFromContactsAsync(int userId, List<int> contactIds)
    {
        if (contactIds is not { Count: > 0 }) return Task.CompletedTask;
        
        return ExecuteInTransactionAsync(async () =>
        {
            await _repository.RemoveContactsBatchAsync(userId, contactIds);
            await _repository.RemoveGroupMembersBatchAsync(userId, contactIds);
        });
    }
    
    public Task RemoveAllUserContactsAsync(int userId, List<int>? excludeIds = null)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            if (excludeIds is not { Count: > 0 })
            {
                await _repository.RemoveAllContactsAsync(userId);
                await _repository.RemoveAllGroupMembersAsync(userId);
            }
            else
            {
                await _repository.RemoveAllContactsExceptAsync(userId, excludeIds);
                await _repository.RemoveAllGroupMembersExceptAsync(userId, excludeIds);
            }
        });
    }

    // Вспомогательный метод для инкапсуляции логики транзакций
    private async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        try
        {
            _uow.Begin();
            await action();
            _uow.Commit();
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }
}