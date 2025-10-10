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
using TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

namespace TelegramMediaRelayBot.TelegramBot;

public class MediaDownloaderService
{
    private readonly IMediaDownloaderFactory _downloaderFactory;
    private readonly IProxyPolicyManager _proxyPolicyManager;
    private readonly IRetryPolicyManager _retryPolicyManager;

    // 1. Конструктор теперь запрашивает все наши "мозги" из DI
    public MediaDownloaderService(
        IMediaDownloaderFactory downloaderFactory,
        IProxyPolicyManager proxyPolicyManager,
        IRetryPolicyManager retryPolicyManager)
    {
        _downloaderFactory = downloaderFactory;
        _proxyPolicyManager = proxyPolicyManager;
        _retryPolicyManager = retryPolicyManager;
    }

    public async Task<DownloadResult> DownloadMedia(string url, DownloadOptions options, CancellationToken ct)
    {
        // 2. Находим всех подходящих "рабочих" (загрузчиков) для URL
        var availableDownloaders = _downloaderFactory.GetDownloadersForUrl(url);

        if (!availableDownloaders.Any())
        {
            return new DownloadResult { Success = false, ErrorMessage = $"No downloader found for URL: {url}" };
        }

        DownloadResult lastResult = new() { Success = false };

        // 3. ВНЕШНИЙ ЦИКЛ: Пробуем каждого "рабочего" по очереди
        foreach (var downloader in availableDownloaders)
        {
            Log.Information("Attempting to download with {DownloaderName}...", downloader.Name);
            
            int attempt = 1;

            // 4. ВНУТРЕННИЙ ЦИКЛ: Реализуем "умные" ретраи для одного "рабочего"
            while (true)
            {
                // 5. ОПРЕДЕЛЯЕМ ПРОКСИ для текущей попытки
                // Получаем базовый адрес прокси из политики загрузчика
                var proxyAddress = _proxyPolicyManager.GetProxyAddress(downloader.Config, url);

                // Если предыдущая попытка провалилась и политика ретраев требует использовать прокси,
                // это переопределит базовую настройку.
                if (lastResult.Modifiers?.UseProxyName != null)
                {
                    // TODO: Реализовать получение адреса прокси по имени из Modifiers
                    Log.Information("Retry policy triggered: switching to proxy '{ProxyName}'", lastResult.Modifiers.UseProxyName);
                    // proxyAddress = ... find proxy address by name ...
                }
                
                // Обновляем опции для этой конкретной попытки
                options.ProxyUrl = proxyAddress;
                
                // 6. ВЫПОЛНЯЕМ ОДНУ ПОПЫТКУ СКАЧИВАНИЯ
                lastResult = await downloader.Download(url, options, ct);
                lastResult.AttemptNumber = attempt;

                // 7. АНАЛИЗИРУЕМ РЕЗУЛЬТАТ
                if (lastResult.Success)
                {
                    Log.Information("Download successful with {DownloaderName} on attempt {Attempt}", downloader.Name, attempt);
                    return lastResult; // Успех! Выходим.
                }

                // Если провал, логируем ошибку
                Log.Warning("Attempt {Attempt} with {DownloaderName} failed: {Error}", attempt, downloader.Name, lastResult.ErrorMessage);
                
                // 8. СПРАШИВАЕМ У ПОЛИТИКИ РЕТРАЕВ, ЧТО ДЕЛАТЬ ДАЛЬШЕ
                var decision = _retryPolicyManager.Decide(lastResult, attempt);
                
                if (decision.ShouldRetry)
                {
                    // Нужно повторить. Передаем модификаторы в следующую итерацию.
                    lastResult.Modifiers = decision.Modifiers;
                    
                    Log.Information("Waiting {Delay}s before next attempt...", decision.Delay.TotalSeconds);
                    await Task.Delay(decision.Delay, ct);
                    attempt++;
                }
                else
                {
                    // Повторять не нужно. Выходим из внутреннего цикла и переходим к следующему загрузчику.
                    Log.Warning("Retry policy decided not to retry with {DownloaderName}.", downloader.Name);
                    break;
                }
            }
        }

        // 9. Если мы прошли всех "рабочих" и никто не справился
        Log.Error("All downloaders and retry policies failed for URL: {Url}", url);
        return new DownloadResult { Success = false, ErrorMessage = "All downloaders failed." };
    }
}