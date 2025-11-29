using System.Text.RegularExpressions;
using TelegramMediaRelayBot.Domain.Interfaces;

namespace TelegramMediaRelayBot.Services;

public class CredentialProvider : ICredentialProvider
{
    private readonly string _secretsDirectory;
    private static readonly Regex _envVarRegex = new Regex(@"^\$\{(.+)\}$", RegexOptions.Compiled);

    public CredentialProvider()
    {
        _secretsDirectory = Path.Combine(AppContext.BaseDirectory, "Secrets");
        if (!Directory.Exists(_secretsDirectory))
        {
            try 
            { 
                Directory.CreateDirectory(_secretsDirectory); 
            }
            catch 
            {}
        }
    }

    /// <summary>
    /// Ищет файл в папке Secrets. Возвращает полный путь или null, если файл не найден.
    /// </summary>
    public string? GetCookieFilePath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(_secretsDirectory, fileName));
            if (!fullPath.StartsWith(_secretsDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return System.IO.File.Exists(fullPath) ? fullPath : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Если строка формата ${VAR_NAME} - берет значение из переменных окружения.
    /// Иначе возвращает строку как есть.
    /// </summary>
    public string? ResolveSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        var match = _envVarRegex.Match(value.Trim());
        if (match.Success)
        {
            var envVarName = match.Groups[1].Value;
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            return envValue;
        }

        return value;
    }
}