namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IPrivacySettingsUoW
{
    Task<bool> SetPrivacyRule(int userId, string type, string action, bool isActive, string actionCondition);
    Task<bool> SetPrivacyRuleToDisabled(int userId, string type);
}