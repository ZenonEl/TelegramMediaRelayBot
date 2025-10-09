namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IPrivacySettingsTargetsUoW
{
    // Имена методов, которые отражают бизнес-логику
    Task<bool> SetPrivacyRuleTarget(int userId, int privacySettingId, string targetType, string targetValue);
    Task<bool> SetToRemovePrivacyRuleTarget(int privacySettingId, string targetValue);
}