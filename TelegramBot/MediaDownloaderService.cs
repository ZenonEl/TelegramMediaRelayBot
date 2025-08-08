// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;


namespace TelegramMediaRelayBot;

public class MediaDownloaderService
{
    private readonly IMediaDownloaderFactory _downloaderFactory;
    private readonly IOptionsMonitor<BotConfiguration> _botConfig;
    
    public MediaDownloaderService(IMediaDownloaderFactory downloaderFactory, IOptionsMonitor<BotConfiguration> botConfig)
    {
        _downloaderFactory = downloaderFactory;
        _botConfig = botConfig;
    }
    
    public async Task<List<byte[]>?> DownloadMedia(
        ITelegramBotClient botClient, 
        string videoUrl, 
        Message statusMessage, 
        CancellationToken cancellationToken)
    {
        // Используем fallback систему
        return await DownloadMediaWithFallback(botClient, videoUrl, statusMessage, cancellationToken);
    }
    
    public async Task<List<byte[]>?> DownloadMediaWithFallback(
        ITelegramBotClient botClient, 
        string videoUrl, 
        Message statusMessage, 
        CancellationToken cancellationToken)
    {
        // Получаем все подходящие загрузчики для этого URL
        var downloaders = _downloaderFactory.GetDownloadersForUrl(videoUrl).ToList();
        
        if (!downloaders.Any())
        {
            Log.Error("No downloaders found for {Url}", videoUrl);
            return null;
        }
        
        Log.Information("Found {Count} downloaders for {Url}: {Downloaders}", 
            downloaders.Count, videoUrl, string.Join(", ", downloaders.Select(d => d.Name)));
        
        foreach (var downloader in downloaders)
        {
            try
            {
                Log.Information("Trying downloader {Downloader} for {Url}", downloader.Name, videoUrl);
                
                var options = new DownloadOptions
                {
                    ProxyUrl = _botConfig.CurrentValue.Proxy,
                    Timeout = TimeSpan.FromMinutes(10),
                    MaxRetries = 3,
                    BotClient = botClient,
                    StatusMessage = statusMessage
                };
                
                var result = await downloader.DownloadAsync(videoUrl, options, cancellationToken);
                
                if (result.Success)
                {
                    Log.Information("Download successful with {Downloader} for {Url}", downloader.Name, videoUrl);
                    return result.MediaFiles;
                }
                else
                {
                    Log.Warning("Downloader {Downloader} failed for {Url}: {Error}", 
                        downloader.Name, videoUrl, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Downloader {Downloader} failed for {Url}", downloader.Name, videoUrl);
                continue;
            }
        }
        
        Log.Error("All downloaders failed for {Url}", videoUrl);
        return null;
    }
} 