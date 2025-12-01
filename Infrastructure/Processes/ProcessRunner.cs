// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;

namespace TelegramMediaRelayBot.Infrastructure.Processes;

public class ProcessRunner : IProcessRunner
{
    // --- НОВЫЕ ПОЛЯ И КОНСТРУКТОР ---
    private readonly DownloaderConfigRoot _config;

    public ProcessRunner(IOptionsMonitor<DownloaderConfigRoot> configMonitor)
    {
        _config = configMonitor.CurrentValue;
        // TODO: Подписаться на OnChange
    }

    public async Task<CommandResult> RunAsync(ProcessRunOptions options, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.FileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in options.Arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

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
        catch (OperationCanceledException)
        {
            exited = false;
        }


        if (!exited)
        {
            try { process.Kill(true); }
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
        var enableProgressOutput = onOutput != null && _config.GlobalSettings.DownloadProgressLogLevel == ProgressLogLevel.Verbose;

        try
        {
            while (!ct.IsCancellationRequested && await reader.ReadLineAsync(ct) is { } line)
            {
                lines.Add(line);

                // Вызываем "подписчика" только если разрешено
                if (enableProgressOutput)
                {
                    onOutput?.Invoke(line);
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
