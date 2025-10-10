using System.Text;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;

public class ArgumentBuilder : IArgumentBuilder
{
    public string Build(string template, ArgumentBuilderContext context)
    {
        var sb = new StringBuilder(template);

        // Основные подстановки
        sb.Replace("{Url}", context.Url); // URL обычно не нужно экранировать
        sb.Replace("{OutputPath}", SanitizePath(context.OutputPath));

        // Опциональные параметры
        HandleOptionalArgument(sb, "--proxy", "{Proxy}", context.ProxyAddress);
        HandleOptionalArgument(sb, "--cookies", "{CookiesPath}", context.CookiesPath);
        HandleOptionalArgument(sb, "--format", "{Format}", context.FormatSelection);
        
        return sb.ToString().Replace("  ", " ").Trim();
    }

    /// <summary>
    /// Обрабатывает опциональный аргумент. Если значение есть - подставляет. Если нет - удаляет флаг.
    /// </summary>
    private void HandleOptionalArgument(StringBuilder sb, string flag, string placeholder, string? value)
    {
        if (sb.ToString().Contains(flag)) // Проверяем, есть ли вообще флаг в шаблоне
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Значение есть, заменяем плейсхолдер
                sb.Replace(placeholder, SanitizePath(value));
            }
            else
            {
                // Значения нет, удаляем флаг целиком
                sb.Replace(flag, string.Empty);
                sb.Replace(placeholder, string.Empty);
            }
        }
        else if (!string.IsNullOrEmpty(value))
        {
             // Если флага в шаблоне нет, но значение передано (для гибкости), добавляем в конец
            sb.Append($" {flag} {SanitizePath(value)}");
        }
    }
    
    /// <summary>
    /// Безопасно экранирует путь для командной строки.
    /// </summary>
    private string SanitizePath(string path)
    {
        // Если путь уже в кавычках, ничего не делаем
        if (path.StartsWith('"') && path.EndsWith('"'))
        {
            return path;
        }
        // Добавляем кавычки, чтобы обработать пути с пробелами
        return $"\"{path}\"";
    }
}