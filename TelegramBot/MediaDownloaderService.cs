// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

namespace TelegramMediaRelayBot.TelegramBot;

public class MediaDownloaderService
{
    private readonly IMediaDownloaderFactory _downloaderFactory;
    private readonly IProxyPolicyManager _proxyPolicyManager;
    private readonly IRetryPolicyManager _retryPolicyManager;
    private readonly DownloaderConfigRoot _config; // Получаем доступ ко всей конфигурации

    public MediaDownloaderService(
        IMediaDownloaderFactory downloaderFactory,
        IProxyPolicyManager proxyPolicyManager,
        IRetryPolicyManager retryPolicyManager,
        IOptionsMonitor<DownloaderConfigRoot> configMonitor)
    {
        _downloaderFactory = downloaderFactory;
        _proxyPolicyManager = proxyPolicyManager;
        _retryPolicyManager = retryPolicyManager;
        _config = configMonitor.CurrentValue;
        // TODO: Подписаться на OnChange для обновления _config
    }

    public async Task<DownloadResult> DownloadMedia(string url, DownloadOptions options, CancellationToken ct)
    {
        var availableDownloaders = _downloaderFactory.GetDownloadersForUrl(url);
        if (!availableDownloaders.Any())
        {
            return new DownloadResult { Success = false, ErrorMessage = $"No downloader found for URL: {url}" };
        }

        var lastErrorResult = new DownloadResult { Success = false, ErrorMessage = "All downloaders failed." };

        foreach (var downloader in availableDownloaders)
        {
            Log.Information("Attempting to download with {DownloaderName}...", downloader.Name);
            
            var attemptResult = new DownloadResult { Success = false };
            int attempt = 1;

            while (true)
            {
                var proxyAddress = _proxyPolicyManager.GetProxyAddress(downloader.Config, url);
                
                if (attemptResult.Modifiers?.UseProxyName != null)
                {
                    // ... логика смены прокси ...
                }
                
                // --- ГЛАВНОЕ ИЗМЕНЕНИЕ ---
                // Мы больше НЕ трогаем options.OnProgress.
                // Мы просто передаем ОРИГИНАЛЬНЫЙ объект options дальше.
                options.ProxyUrl = proxyAddress;
                options.LastResult = attemptResult;
                
                attemptResult = await downloader.Download(url, options, ct);
                attemptResult.AttemptNumber = attempt;
                
                if (attemptResult.Success)
                {
                    Log.Information("Download successful with {DownloaderName} on attempt {Attempt}", downloader.Name, attempt);
                    return attemptResult;
                }
                
                Log.Warning("Attempt {Attempt} with {DownloaderName} failed: {Error}", attempt, downloader.Name, attemptResult.ErrorMessage);
                lastErrorResult = attemptResult;

                var decision = _retryPolicyManager.Decide(lastErrorResult, attempt);
                
                if (decision.ShouldRetry)
                {
                    attemptResult.Modifiers = decision.Modifiers;
                    await Task.Delay(decision.Delay, ct);
                    attempt++;
                }
                else
                {
                    Log.Warning("Retry policy decided not to retry with {DownloaderName}.", downloader.Name);
                    break;
                }
            }
        }
        
        Log.Error("All downloaders and retry policies failed for URL: {Url}", url);
        return lastErrorResult;
    }
}