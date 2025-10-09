namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IPrivacySettingsTargetsRepository
{
    // Методы из Setter'а
    Task<int> UpsertTarget(int userId, int privacySettingId, string targetType, string targetValue);
    Task<int> RemoveTarget(int privacySettingId, string targetValue);

    // Методы из Getter'а
    Task<bool> CheckTargetExists(int userId, string type);
    Task<List<string>> GetAllUserTargets(int userId);
}