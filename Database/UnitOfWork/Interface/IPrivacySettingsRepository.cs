namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IPrivacySettingsRepository
{
    Task<int> UpsertRule(int userId, string type, string action, bool isActive, string actionCondition);
    Task<int> DisableRule(int userId, string type);
}