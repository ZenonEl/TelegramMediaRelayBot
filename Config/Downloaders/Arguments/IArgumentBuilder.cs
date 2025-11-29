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
    /// Строит список аргументов, заменяя токены значениями из контекста.
    /// </summary>
    /// <param name="argumentTemplates">Шаблоны аргументов из конфига.</param>
    /// <param name="context">Контекст с путями и ссылками.</param>
    /// <param name="authConfig">Конфигурация авторизации (опционально).</param>
    /// <returns>Готовый список аргументов для процесса.</returns>
    List<string> Build(
        List<string> argumentTemplates, 
        ArgumentBuilderContext context, 
        AuthenticationConfig? authConfig = null);
}