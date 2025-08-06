// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Text.RegularExpressions;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders;

public class YtDlpDownloader : BaseMediaDownloader
{
    private readonly string _executablePath;
    private readonly string[] _checkCommands;
    private readonly string[] _defaultArguments;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly string _outputPattern;
    private readonly string _progressPattern;
    
    public override string Name => "YtDlp";
    public override int Priority => _configuration.GetValue($"Downloaders:{Name}:Priority", 100);
    public override MediaType SupportedMediaTypes => MediaType.Video | MediaType.Audio;
    public override bool IsEnabled => _configuration.GetValue($"Downloaders:{Name}:Enabled", true);
    
    public YtDlpDownloader(IConfiguration configuration) : base(configuration)
    {
        _executablePath = _configuration.GetValue($"Downloaders:{Name}:Path", "yt-dlp")!;
        _checkCommands = _configuration.GetSection($"Downloaders:{Name}:CheckCommands").Get<string[]>() ?? new[] { "--dry-run" };
        _defaultArguments = _configuration.GetSection($"Downloaders:{Name}:DefaultArguments").Get<string[]>() ?? new string[0];
        _timeout = TimeSpan.Parse(_configuration.GetValue($"Downloaders:{Name}:Timeout", "00:10:00")!);
        _maxRetries = _configuration.GetValue($"Downloaders:{Name}:MaxRetries", 3);
        _outputPattern = _configuration.GetValue($"Downloaders:{Name}:OutputPattern", "\\[download\\] Destination: (.+)")!;
        _progressPattern = _configuration.GetValue($"Downloaders:{Name}:ProgressPattern", "\\[download\\]")!;
    }
    
    protected override async Task<DownloadCapability> CheckCapabilityInternalAsync(string url, CancellationToken ct)
    {
        foreach (var command in _checkCommands)
        {
            try
            {
                var result = await ExecuteCommandAsync(_executablePath, $"{command} {url}", _timeout, ct);
                if (result.ExitCode == 0)
                {
                    return new DownloadCapability
                    {
                        CanDownload = true,
                        MediaTypes = ParseMediaTypesFromOutput(result.Output),
                        Metadata = ParseMetadataFromOutput(result.Output)
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Check command {Command} failed for {Url}", command, url);
                continue;
            }
        }
        
        return new DownloadCapability { CanDownload = false };
    }
    
    protected override async Task<DownloadResult> DownloadInternalAsync(string url, DownloadOptions options, CancellationToken ct)
    {
        var tempDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirPath);
        
        try
        {
                    // Формируем аргументы
        var arguments = BuildArguments(url, tempDirPath, options);
        
        Log.Debug("YtDlp temp directory: {TempDir}", tempDirPath);
        Log.Debug("YtDlp arguments: {Arguments}", arguments);
        
        // Выполняем команду
        var result = await ExecuteCommandWithProgressAsync(_executablePath, arguments, options, ct);
        
        Log.Debug("YtDlp output: {Output}", result.Output);
        Log.Debug("YtDlp error output: {ErrorOutput}", result.ErrorOutput);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"yt-dlp failed with exit code {result.ExitCode}: {result.ErrorOutput}");
        }
        
        // Ищем путь к скачанному файлу
        var filePath = ExtractFilePath(result.Output);
        Log.Debug("YtDlp extracted file path: {FilePath}", filePath);
        
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            // Проверяем содержимое временной директории
            var files = Directory.GetFiles(tempDirPath);
            Log.Debug("YtDlp found {Count} files in temp directory: {Files}", files.Length, string.Join(", ", files));
            throw new InvalidOperationException("Could not find downloaded file");
        }
        
        // Читаем файл
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
        Log.Debug("YtDlp read file: {File}, size: {Size} bytes", filePath, fileBytes.Length);
        
        return new DownloadResult
        {
            Success = true,
            MediaFiles = new List<byte[]> { fileBytes },
            MediaType = SupportedMediaTypes,
            Duration = result.Duration,
            FileSize = fileBytes.Length
        };
        }
        finally
        {
            // Очищаем временную директорию
            if (Directory.Exists(tempDirPath))
            {
                try
                {
                    Directory.Delete(tempDirPath, true);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete temp directory {TempDir}", tempDirPath);
                }
            }
        }
    }
    
    private string BuildArguments(string url, string tempDirPath, DownloadOptions options)
    {
        var arguments = new List<string>();
        
        // Добавляем аргументы по умолчанию
        foreach (var arg in _defaultArguments)
        {
            var processedArg = arg
                .Replace("{Proxy}", options.ProxyUrl ?? "")
                .Replace("{OutputPath}", tempDirPath);
            
            // Добавляем все аргументы, даже пустые
            arguments.Add(processedArg);
        }
        
        // Добавляем URL в конец
        arguments.Add(url);
        
        var result = string.Join(" ", arguments);
        Log.Debug("YtDlp arguments: {Arguments}", result);
        return result;
    }
    
    private async Task<CommandResult> ExecuteCommandWithProgressAsync(string executablePath, string arguments, DownloadOptions options, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();
        
        process.Start();
        
        var outputLines = new List<string>();
        var errorLines = new List<string>();

        var readOutputTask = ReadLinesWithProgressAsync(process.StandardOutput, outputLines, options, ct);
        var readErrorTask = ReadLinesAsync(process.StandardError, errorLines, ct);

        var timeoutTask = Task.Delay(_timeout, ct);
        var processTask = process.WaitForExitAsync(ct);

        var completedTask = await Task.WhenAny(processTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            try
            {
                process.Kill();
            }
            catch { }
            
            throw new TimeoutException($"yt-dlp execution timed out after {_timeout.TotalSeconds} seconds");
        }

        await Task.WhenAll(readOutputTask, readErrorTask);
        stopwatch.Stop();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            Output = string.Join("\n", outputLines),
            ErrorOutput = string.Join("\n", errorLines),
            Duration = stopwatch.Elapsed
        };
    }
    
    private async Task ReadLinesWithProgressAsync(StreamReader reader, List<string> lines, DownloadOptions options, CancellationToken ct)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                lines.Add(line);
                
                // Обновляем прогресс если есть бот клиент
                if (options.BotClient != null && options.StatusMessage != null && 
                    Regex.IsMatch(line, _progressPattern))
                {
                    try
                    {
                        var progressText = RemoveUntilDownload(line);
                        await options.BotClient.EditMessageText(
                            options.StatusMessage.Chat.Id, 
                            options.StatusMessage.MessageId, 
                            progressText,
                            cancellationToken: ct);
                        
                        await Task.Delay(Config.videoGetDelay, ct);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Error updating progress message");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading output lines");
        }
    }
    
    private string RemoveUntilDownload(string line)
    {
        int startIndex = line.IndexOf("[download]");
        return startIndex != -1 ? line.Substring(startIndex) : line;
    }
    
    private string? ExtractFilePath(string output)
    {
        var match = Regex.Match(output, _outputPattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
} 