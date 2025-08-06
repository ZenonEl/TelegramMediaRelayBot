// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Diagnostics;
using System.Text.RegularExpressions;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders;

public abstract class BaseMediaDownloader : IMediaDownloader
{
    protected readonly IConfiguration _configuration;
    protected readonly HashSet<string> _knownSupportedDomains = new();
    protected readonly List<Regex> _urlPatterns = new();
    
    public abstract string Name { get; }
    public abstract int Priority { get; }
    public abstract MediaType SupportedMediaTypes { get; }
    public abstract bool IsEnabled { get; }
    
    protected BaseMediaDownloader(IConfiguration configuration)
            {
            _configuration = configuration;
            InitializePatterns();
        }
    
    protected virtual void InitializePatterns()
    {
        var patterns = _configuration.GetSection($"Downloaders:{Name}:UrlPatterns").Get<string[]>();
        if (patterns != null)
        {
            foreach (var pattern in patterns)
            {
                _urlPatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }
    }
    
    public virtual bool CanHandle(string url)
    {
        var uri = new Uri(url);
        var host = uri.Host.ToLowerInvariant();
        
        // Проверяем известные поддерживаемые домены
        if (_knownSupportedDomains.Contains(host))
            return true;
            
        // Проверяем паттерны URL
        if (_urlPatterns.Any(pattern => pattern.IsMatch(url)))
            return true;
            
        // Для неизвестных доменов - возвращаем true для возможности проверки
        return true;
    }
    
    public async Task<DownloadCapability> CheckCapabilityAsync(string url, CancellationToken ct)
    {
        var uri = new Uri(url);
        var host = uri.Host.ToLowerInvariant();
        
        try
        {
            var capability = await CheckCapabilityInternalAsync(url, ct);
            
            // Запоминаем только поддерживаемые домены
            if (capability.CanDownload)
            {
                _knownSupportedDomains.Add(host);
            }
            
            return capability;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking capability for {Url} with {Downloader}", url, Name);
            return new DownloadCapability
            {
                CanDownload = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    public async Task<DownloadResult> DownloadAsync(string url, DownloadOptions options, CancellationToken ct)
    {
        try
        {
            Log.Information("Starting download with {Downloader} for {Url}", Name, url);
            var result = await DownloadInternalAsync(url, options, ct);
            Log.Information("Download completed with {Downloader} for {Url}", Name, url);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Download failed with {Downloader} for {Url}", Name, url);
            return new DownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    protected abstract Task<DownloadCapability> CheckCapabilityInternalAsync(string url, CancellationToken ct);
    protected abstract Task<DownloadResult> DownloadInternalAsync(string url, DownloadOptions options, CancellationToken ct);
    
    protected async Task<CommandResult> ExecuteCommandAsync(string executablePath, string arguments, TimeSpan timeout, CancellationToken ct)
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

        var readOutputTask = ReadLinesAsync(process.StandardOutput, outputLines, ct);
        var readErrorTask = ReadLinesAsync(process.StandardError, errorLines, ct);

        var timeoutTask = Task.Delay(timeout, ct);
        var processTask = process.WaitForExitAsync(ct);

        var completedTask = await Task.WhenAny(processTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            try
            {
                process.Kill();
            }
            catch { }
            
            throw new TimeoutException($"Command execution timed out after {timeout.TotalSeconds} seconds");
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
    
    protected async Task ReadLinesAsync(StreamReader reader, List<string> lines, CancellationToken ct)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                lines.Add(line);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading output lines");
        }
    }
    
    protected MediaType ParseMediaTypesFromOutput(string output)
    {
        // Базовая реализация - можно переопределить в наследниках
        if (output.Contains("video") || output.Contains("mp4") || output.Contains("avi"))
            return MediaType.Video;
        if (output.Contains("image") || output.Contains("jpg") || output.Contains("png"))
            return MediaType.Image;
        if (output.Contains("audio") || output.Contains("mp3") || output.Contains("wav"))
            return MediaType.Audio;
            
        return MediaType.None;
    }
    
    protected Dictionary<string, object> ParseMetadataFromOutput(string output)
    {
        var metadata = new Dictionary<string, object>();
        
        // Извлекаем размер файла
        var sizeMatch = Regex.Match(output, @"(\d+(?:\.\d+)?)\s*(MB|KB|GB)", RegexOptions.IgnoreCase);
        if (sizeMatch.Success)
        {
            metadata["FileSize"] = sizeMatch.Value;
        }
        
        // Извлекаем длительность
        var durationMatch = Regex.Match(output, @"(\d{1,2}):(\d{2}):(\d{2})");
        if (durationMatch.Success)
        {
            metadata["Duration"] = durationMatch.Value;
        }
        
        return metadata;
    }
} 