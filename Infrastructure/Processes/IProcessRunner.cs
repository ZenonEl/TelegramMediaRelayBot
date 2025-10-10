namespace TelegramMediaRelayBot.Infrastructure.Processes;

/// <summary>
/// Представляет результат выполнения внешней команды.
/// </summary>
public class CommandResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string ErrorOutput { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public bool TimedOut { get; init; }
}

/// <summary>
/// Опции для запуска процесса.
/// </summary>
public class ProcessRunOptions
{
    /// <summary>
    /// Имя или путь к исполняемому файлу.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Аргументы командной строки.
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Максимальное время выполнения.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// (Издатель) Делегат, который будет вызываться для каждой строки из stdout/stderr.
    /// </summary>
    public Action<string>? OnOutputLine { get; init; }
}

/// <summary>
/// Сервис для запуска и управления внешними процессами.
/// </summary>
public interface IProcessRunner
{
    Task<CommandResult> RunAsync(ProcessRunOptions options, CancellationToken ct);
}