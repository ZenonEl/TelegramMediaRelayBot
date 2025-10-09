namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IDefaultActionTargetsRepository
{
    Task<int> AddTarget(int userId, int actionId, string targetType, int targetId);
    Task<int> RemoveAllTargets(int userId, string targetType, int actionId);
}