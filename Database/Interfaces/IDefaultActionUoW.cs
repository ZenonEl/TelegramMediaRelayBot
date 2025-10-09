namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IDefaultActionUoW
{
    Task<bool> SetAutoSendVideoCondition(int userId, string actionCondition, string type);
    Task<bool> SetAutoSendVideoAction(int userId, string action, string type);
}