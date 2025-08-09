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
using System.Diagnostics;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders;

public class GalleryDlDownloader : BaseMediaDownloader
{
    private string ExecutablePath => _configuration.GetValue($"Downloaders:{Name}:Path", "gallery-dl")!;
    private string[] AlternativePaths => _configuration.GetSection($"Downloaders:{Name}:AlternativePaths").Get<string[]>() ?? Array.Empty<string>();
    private string[] CheckCommands => _configuration.GetSection($"Downloaders:{Name}:CheckCommands").Get<string[]>() ?? new[] { "--list-urls" };
    private string[] DefaultArguments => _configuration.GetSection($"Downloaders:{Name}:DefaultArguments").Get<string[]>() ?? Array.Empty<string>();
    private TimeSpan Timeout => TimeSpan.Parse(_configuration.GetValue($"Downloaders:{Name}:Timeout", "00:05:00")!);
    private int MaxRetries => _configuration.GetValue($"Downloaders:{Name}:MaxRetries", 2);
    private string ProgressPattern => _configuration.GetValue($"Downloaders:{Name}:ProgressPattern", "\\[download\\]")!;
    
    public override string Name => "GalleryDl";
    public override int Priority => _configuration.GetValue($"Downloaders:{Name}:Priority", 90);
    public override MediaType SupportedMediaTypes => MediaType.Image;
    public override bool IsEnabled => _configuration.GetValue($"Downloaders:{Name}:Enabled", true);
    
    public GalleryDlDownloader(IConfiguration configuration) : base(configuration) { }
    
    protected override async Task<DownloadCapability> CheckCapabilityInternalAsync(string url, CancellationToken ct)
    {
        foreach (var command in CheckCommands)
        {
            try
            {
                var result = await ExecuteCommandAsync(ExecutablePath, $"{command} {url}", Timeout, ct);
                if (result.ExitCode == 0)
                {
                    return new DownloadCapability
                    {
                        CanDownload = true,
                        MediaTypes = MediaType.Image,
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
            // Пробуем основной путь
            var result = await TryDownloadWithPathAsync(ExecutablePath, url, tempDirPath, options, ct);
            
            // Если не получилось, пробуем альтернативные пути
            if (!result.Success && AlternativePaths.Length > 0)
            {
                foreach (var alternativePath in AlternativePaths)
                {
                    try
                    {
                        result = await TryDownloadWithPathAsync(alternativePath, url, tempDirPath, options, ct);
                        if (result.Success)
                            break;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Alternative path {Path} failed for {Url}", alternativePath, url);
                    }
                }
            }
            
            return result;
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
    
    private async Task<DownloadResult> TryDownloadWithPathAsync(string executablePath, string url, string tempDirPath, DownloadOptions options, CancellationToken ct)
    {
        // Формируем аргументы
        var arguments = BuildArguments(url, tempDirPath, options);
        
        Log.Debug("GalleryDl temp directory: {TempDir}", tempDirPath);
        Log.Debug("GalleryDl arguments: {Arguments}", arguments);
        
        // Выполняем команду
        var result = await ExecuteCommandWithProgressAsync(executablePath, arguments, options, ct);
        
        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            var outShort = result.Output.Length > 2000 ? result.Output[^2000..] : result.Output;
            Log.Debug("GalleryDl output (tail): {Output}", outShort);
        }
        if (!string.IsNullOrWhiteSpace(result.ErrorOutput))
        {
            var errShort = result.ErrorOutput.Length > 2000 ? result.ErrorOutput[^2000..] : result.ErrorOutput;
            if (ct.IsCancellationRequested)
            {
                Log.Debug("GalleryDl canceled, error output (tail) suppressed");
            }
            else
            {
                Log.Debug("GalleryDl error output (tail): {ErrorOutput}", errShort);
            }
        }
        
        if (result.ExitCode != 0)
        {
            if (ct.IsCancellationRequested)
            {
                return new DownloadResult { Success = false, ErrorMessage = "Canceled" };
            }
            throw new InvalidOperationException($"gallery-dl failed with exit code {result.ExitCode}: {result.ErrorOutput}");
        }
        
        // Читаем все файлы из временной директории
        var files = Directory.GetFiles(tempDirPath);
        Log.Debug("GalleryDl found {Count} files in temp directory: {Files}", files.Length, string.Join(", ", files));
        
        if (files.Length == 0)
        {
            // Проверяем содержимое директории
            var dirContents = Directory.GetDirectories(tempDirPath);
            Log.Debug("GalleryDl temp directory contents: {Contents}", string.Join(", ", dirContents));
            throw new InvalidOperationException("No files were downloaded");
        }
        
        var mediaFiles = new List<byte[]>();
        long totalSize = 0;
        
        foreach (var file in files)
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(file, ct);
            mediaFiles.Add(fileBytes);
            totalSize += fileBytes.Length;
            Log.Debug("GalleryDl read file: {File}, size: {Size} bytes", file, fileBytes.Length);
        }
        
        return new DownloadResult
        {
            Success = true,
            MediaFiles = mediaFiles,
            MediaType = MediaType.Image,
            Duration = result.Duration,
            FileSize = totalSize
        };
    }
    
    private string BuildArguments(string url, string tempDirPath, DownloadOptions options)
    {
        var arguments = new List<string>();
        
        // Добавляем аргументы по умолчанию
        var defaults = DefaultArguments;
        Log.Debug("GalleryDl default arguments: {Args}", string.Join(", ", defaults));
        for (int i = 0; i < defaults.Length; i++)
        {
            var arg = defaults[i];
            
            // Определяем прокси для конкретного сайта
            var siteSpecificProxy = GetSiteSpecificProxy(url, options.ProxyUrl);
            
            // Пропускаем пару --proxy и его значение, если прокси пустой
            if (arg == "--proxy" && string.IsNullOrEmpty(siteSpecificProxy))
            {
                // Пропускаем текущий аргумент и следующий (значение прокси)
                i++;
                Log.Debug("GalleryDl skipping proxy argument pair for {Url}", url);
                continue;
            }
            
            // Обрабатываем User-Agent как единый аргумент
            if (arg == "--user-agent")
            {
                var userAgent = defaults[i + 1];
                arguments.Add(arg);
                arguments.Add($"\"{userAgent}\""); // Оборачиваем в кавычки
                i++; // Пропускаем следующий аргумент (значение User-Agent)
                continue;
            }
            
            var processedArg = arg
                .Replace("{Proxy}", siteSpecificProxy ?? "")
                .Replace("{OutputPath}", tempDirPath);
            
            Log.Debug("GalleryDl processing arg: '{Arg}' -> '{ProcessedArg}'", arg, processedArg);
            arguments.Add(processedArg);
        }
        
        // Добавляем URL в конец
        arguments.Add(url);
        
        var result = string.Join(" ", arguments);
        Log.Debug("GalleryDl arguments: {Arguments}", result);
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

        var timeoutTask = Task.Delay(Timeout, ct);
        var processTask = process.WaitForExitAsync(ct);

        var completedTask = await Task.WhenAny(processTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            try
            {
                process.Kill();
            }
            catch { }
            
            throw new TimeoutException($"gallery-dl execution timed out after {Timeout.TotalSeconds} seconds");
        }

        try
        {
            await Task.WhenAll(readOutputTask, readErrorTask);
        }
        catch (OperationCanceledException)
        {
            // ignore cancellation during read
        }
        stopwatch.Stop();

        return new CommandResult
        {
            ExitCode = process.HasExited ? process.ExitCode : -1,
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
                
                if (options.BotClient != null && options.StatusMessage != null && 
                    Regex.IsMatch(line, ProgressPattern))
                {
                    try
                    {
                        var progressText = RemoveUntilDownload(line);
                        await options.BotClient.EditMessageText(
                            options.StatusMessage.Chat.Id, 
                            options.StatusMessage.MessageId, 
                            progressText,
                            cancellationToken: ct);
                        await Task.Delay(1000, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Error updating progress message");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected when user cancels
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
} 