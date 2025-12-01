// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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
        IEnumerable<IMediaDownloader> availableDownloaders = _downloaderFactory.GetDownloadersForUrl(url);
        if (!availableDownloaders.Any())
        {
            return new DownloadResult { Success = false, ErrorMessage = $"No downloader found for URL: {url}" };
        }

        DownloadResult lastErrorResult = new DownloadResult { Success = false, ErrorMessage = "All downloaders failed." };

        foreach (IMediaDownloader downloader in availableDownloaders)
        {
            Log.Information("Attempting to download with {DownloaderName}...", downloader.Name);

            DownloadResult attemptResult = new DownloadResult { Success = false };
            int attempt = 1;

            while (true)
            {
                string? proxyAddress = _proxyPolicyManager.GetProxyAddress(downloader.Config, url);

                if (attemptResult.Modifiers?.UseProxyName != null)
                {
                    // TODO ... логика смены прокси ...
                }

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

                RetryDecision decision = _retryPolicyManager.Decide(lastErrorResult, attempt);

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
