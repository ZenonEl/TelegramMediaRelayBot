// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Pipeline;
using TelegramMediaRelayBot.Infrastructure.Processes;

namespace TelegramMediaRelayBot.Infrastructure.Pipeline.Executors;

public class CliDownloadExecutor : IDownloadExecutor
{
    private readonly DownloaderDefinition _config; // Конфиг конкретного загрузчика
    private readonly IProcessRunner _processRunner;
    private readonly IArgumentBuilder _argumentBuilder;

    public string Name => _config.Name;

    public CliDownloadExecutor(
        DownloaderDefinition config,
        IProcessRunner processRunner,
        IArgumentBuilder argumentBuilder)
    {
        _config = config;
        _processRunner = processRunner;
        _argumentBuilder = argumentBuilder;
    }

    public async Task<ExecutionResult> ExecuteAsync(DownloadContext context, CancellationToken ct)
    {
        try
        {
            ArgumentBuilderContext builderContext = new ArgumentBuilderContext
            {
                Url = context.OriginalUrl,
                OutputPath = context.OutputDirectory,
                ProxyAddress = context.ActiveProxyUrl,
                CookiesPath = context.AuthData?.CookieFilePath,
                FormatSelection = null // TODO: Добавить в контекст если надо
            };

            // Аутентификация тоже может быть динамической, но пока берем из конфига
            // Важно: если в контексте есть AuthData, можно использовать его приоритетнее конфига
            
            var arguments = _argumentBuilder.Build(_config.ArgumentList, builderContext, _config.Authentication);

            var processOptions = new ProcessRunOptions
            {
                FileName = _config.ExecutablePath,
                Arguments = arguments,
                Timeout = TimeSpan.FromMinutes(10),
                OnOutputLine = (line) => 
                {
                    context.Log($"[CLI] {line}");
                    context.ProgressCallback?.Invoke(line);
                }
            };

            string argsString = string.Join(" ", arguments);
            context.Log($"Executing CLI: {_config.ExecutablePath} with {arguments.Count} args");
            Log.Debug("🚀 [CLI LAUNCH] {Downloader} executing with args: {Args}", Name, argsString);
            CommandResult result = await _processRunner.RunAsync(processOptions, ct);

            if (result.TimedOut)
            {
                return ExecutionResult.Retryable("Process timed out", switchProxy: true);
            }

            if (result.ExitCode != 0)
            {
                // Тут можно добавить анализ stderr, чтобы понять, ошибка фатальная или нет
                // Например, если "Video unavailable" -> Fatal
                // Если "HTTP 429" -> Retryable
                return AnalyzeError(result.ErrorOutput);
            }

            var files = Directory.GetFiles(context.OutputDirectory);
            if (files.Length == 0)
            {
                return ExecutionResult.Retryable("Process finished successfully, but no files found.");
            }

            foreach (var file in files)
            {
                context.ResultFiles.Add(new DownloadedFile
                {
                    FilePath = file,
                    MediaType = DetermineMediaType(file)
                });
            }

            return ExecutionResult.Success();
        }
        catch (OperationCanceledException)
        {
            return ExecutionResult.Fatal("Operation canceled by user.");
        }
        catch (Exception ex)
        {
            return ExecutionResult.Retryable($"Unexpected error: {ex.Message}");
        }
    }

    private ExecutionResult AnalyzeError(string errorOutput)
    {
        if (errorOutput.Contains("Unsupported URL") || errorOutput.Contains("No video formats found"))
        {
            return ExecutionResult.NotSupported("URL not supported or content missing.");
        }
        return ExecutionResult.Retryable(errorOutput, switchProxy: true);
    }

    private MediaType DetermineMediaType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".mp4" or ".mkv" or ".webm" or ".mov" => MediaType.Video,
            ".jpg" or ".jpeg" or ".png" or ".webp" => MediaType.Image,
            ".mp3" or ".m4a" or ".wav" => MediaType.Audio,
            _ => MediaType.None
        };
    }
}