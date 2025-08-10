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
    private readonly IOptionsMonitor<TelegramMediaRelayBot.Config.DownloadingConfiguration> _downloadingConfig;
    
    public MediaDownloaderService(
        IMediaDownloaderFactory downloaderFactory,
        IOptionsMonitor<BotConfiguration> botConfig,
        IOptionsMonitor<TelegramMediaRelayBot.Config.DownloadingConfiguration> downloadingConfig)
    {
        _downloaderFactory = downloaderFactory;
        _botConfig = botConfig;
        _downloadingConfig = downloadingConfig;
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
        // Префлайт по размеру (если включено). При отмене пользователем не редактируем сообщение
        if (await ShouldSkipByExternalSizeAsync(videoUrl, cancellationToken))
        {
            try
            {
                await botClient.EditMessageText(
                    statusMessage.Chat.Id,
                    statusMessage.MessageId,
                    $"❌ Файл слишком большой для загрузки источником (>{_downloadingConfig.CurrentValue.ExternalDownloadMaxSizeMb} MB).",
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignore when canceled
            }
            catch { }
            return null;
        }

        // Получаем все подходящие загрузчики для этого URL
        var downloaders = _downloaderFactory.GetDownloadersForUrl(videoUrl).ToList();
        
        if (!downloaders.Any())
        {
            Log.Error("No downloaders found for {Url}", videoUrl);
            return null;
        }
        
        Log.Information("Found {Count} downloaders for {Url}: {Names}", 
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
                    // Если отмена — прекращаем цепочку без предупреждений и ошибок
                    if (string.Equals(result.ErrorMessage, "Canceled", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Information("Download canceled for {Url}", videoUrl);
                        return null;
                    }
                    Log.Warning("Downloader {Downloader} failed for {Url}: {Error}", 
                        downloader.Name, videoUrl, result.ErrorMessage);
                }
            }
            catch (OperationCanceledException)
            {
                // user canceled: stop fallback chain silently
                return null;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Downloader {Downloader} failed for {Url}", downloader.Name, videoUrl);
                continue;
            }
        }
        
        // Если дошли сюда без успеха и без явной отмены — считаем, что все загрузчики действительно не смогли
        Log.Error("All downloaders failed for {Url}", videoUrl);
        return null;
    }

    private async Task<bool> ShouldSkipByExternalSizeAsync(string url, CancellationToken ct)
    {
        var cfg = _downloadingConfig.CurrentValue;
        if (!cfg.PreflightEnabled || cfg.ExternalDownloadMaxSizeMb <= 0)
            return false;

        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = System.Net.DecompressionMethods.All };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode) return false;

            if (resp.Content.Headers.ContentLength.HasValue)
            {
                var bytes = resp.Content.Headers.ContentLength.Value;
                var mb = bytes / (1024.0 * 1024.0);
                if (mb > cfg.ExternalDownloadMaxSizeMb)
                {
                    Log.Information("Preflight skip: external size {Size:F1}MB exceeds cap {Cap}MB for {Url}", mb, cfg.ExternalDownloadMaxSizeMb, url);
                    return true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Preflight canceled for {Url}", url);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Preflight HEAD failed for {Url}", url);
        }

        return false;
    }
} 