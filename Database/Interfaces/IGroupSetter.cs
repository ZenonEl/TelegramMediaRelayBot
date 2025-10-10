namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IGroupUoW
{
    Task<bool> SetNewGroup(int userId, string groupName, string description);
    Task<bool> SetGroupName(int groupId, string groupName);
    Task<bool> SetGroupDescription(int groupId, string description);
    Task<bool> SetIsDefaultGroup(int groupId);
    Task<bool> SetDeleteGroup(int groupId);
}