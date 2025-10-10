using TelegramMediaRelayBot.Config.Downloaders;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;

/// <summary>
/// Содержит все данные, необходимые для построения строки аргументов.
/// </summary>
public class ArgumentBuilderContext
{
    /// <summary>
    /// URL для скачивания.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Путь к временной папке для сохранения файлов.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Адрес прокси-сервера (если нужно). Может быть null.
    /// </summary>
    public string? ProxyAddress { get; init; }

    /// <summary>
    /// Путь к файлу cookies (если нужно). Может быть null.
    /// </summary>
    public string? CookiesPath { get; init; }

    /// <summary>
    /// Строка для выбора формата видео/аудио (для yt-dlp). Может быть null.
    /// </summary>
    public string? FormatSelection { get; init; }
}

/// <summary>
/// Сервис для построения строки аргументов для внешних загрузчиков.
/// </summary>
public interface IArgumentBuilder
{
    /// <summary>
    /// Строит строку аргументов на основе шаблона и контекста.
    /// </summary>
    /// <param name="template">Шаблон из конфигурации (DownloaderDefinition.ArgumentTemplate).</param>
    /// <param name="context">Данные для подстановки.</param>
    /// <returns>Готовая строка аргументов.</returns>
    string Build(string template, ArgumentBuilderContext context);
}