namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IDefaultActionRepository
{
    Task<int> UpsertActionCondition(int userId, string actionCondition, string type);
    Task<int> UpsertAction(int userId, string action, string type);
}
