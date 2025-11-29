namespace TelegramMediaRelayBot.Domain.Interfaces;

public interface ICredentialProvider
{
    /// <summary>
    /// Возвращает полный путь к файлу кук, проверяя папку Secrets.
    /// </summary>
    string? GetCookieFilePath(string? fileName);

    /// <summary>
    /// Разрешает секрет (заменяет ${ENV_VAR} на значение).
    /// </summary>
    string? ResolveSecret(string? value);
}