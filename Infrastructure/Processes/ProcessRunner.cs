using System.Diagnostics;

namespace TelegramMediaRelayBot.Infrastructure.Processes;

public class ProcessRunner : IProcessRunner
{
    public async Task<CommandResult> RunAsync(ProcessRunOptions options, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.FileName,
            Arguments = options.Arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();

        var outputLines = new List<string>();
        var errorLines = new List<string>();

        process.Start();

        var readOutputTask = ReadStreamAsync(process.StandardOutput, outputLines, options.OnOutputLine, ct);
        var readErrorTask = ReadStreamAsync(process.StandardError, errorLines, options.OnOutputLine, ct);

        bool exited;
        try
        {
            await process.WaitForExitAsync(ct).WaitAsync(options.Timeout, ct);
            exited = true;
        }
        catch (TimeoutException)
        {
            exited = false;
        }

        if (!exited)
        {
            try { process.Kill(true); } // true - убить дерево процессов
            catch (Exception ex) { Log.Warning(ex, "Failed to kill process tree for {FileName}", options.FileName); }
        }

        // Даем немного времени, чтобы завершить чтение потоков после завершения процесса
        await Task.WhenAll(readOutputTask, readErrorTask).WaitAsync(TimeSpan.FromSeconds(5));

        stopwatch.Stop();

        return new CommandResult
        {
            ExitCode = exited ? process.ExitCode : -1,
            Output = string.Join("\n", outputLines),
            ErrorOutput = string.Join("\n", errorLines),
            Duration = stopwatch.Elapsed,
            TimedOut = !exited
        };
    }

    private async Task ReadStreamAsync(StreamReader reader, List<string> lines, Action<string>? onOutput, CancellationToken ct)
    {
        try
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line != null)
                {
                    lines.Add(line);
                    onOutput?.Invoke(line); // "Кричим в воздух", если есть подписчик
                }
            }
        }
        catch (OperationCanceledException) { /* Ожидаемое поведение при отмене */ }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading process output stream.");
        }
    }
}