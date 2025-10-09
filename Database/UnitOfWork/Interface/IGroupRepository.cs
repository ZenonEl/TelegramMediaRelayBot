namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IGroupRepository
{
    Task<int> CreateGroup(int userId, string groupName, string description);
    Task<int> UpdateGroupName(int groupId, string groupName);
    Task<int> UpdateGroupDescription(int groupId, string description);
    Task<int> ToggleDefaultStatus(int groupId);
    Task<int> DeleteGroup(int groupId);
}