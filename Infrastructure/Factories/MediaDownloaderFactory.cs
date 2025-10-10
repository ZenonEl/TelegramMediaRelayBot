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
using TelegramMediaRelayBot.Infrastructure.Downloaders;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;
using TelegramMediaRelayBot.Infrastructure.Processes;

namespace TelegramMediaRelayBot.Infrastructure.Factories;

public class MediaDownloaderFactory : IMediaDownloaderFactory
{
    private DownloaderConfigRoot _config;
    private readonly IProcessRunner _processRunner;
    private readonly IArgumentBuilder _argumentBuilder;
    private readonly List<IMediaDownloader> _downloaders;

    public MediaDownloaderFactory(
        // 1. Получаем всю нашу строго типизированную конфигурацию через IOptionsMonitor
        IOptionsMonitor<DownloaderConfigRoot> configMonitor,
        // 2. Получаем наши новые сервисы из DI
        IProcessRunner processRunner,
        IArgumentBuilder argumentBuilder)
    {
        // IOptionsMonitor позволяет отслеживать изменения в downloader-config.yaml "на лету"
        _config = configMonitor.CurrentValue;
        _processRunner = processRunner;
        _argumentBuilder = argumentBuilder;
        
        // 3. Создаем экземпляры загрузчиков при старте
        _downloaders = new List<IMediaDownloader>();
        InitializeDownloaders();

        // 4. (Опционально) Подписываемся на изменения конфига, чтобы пересоздавать загрузчики при Hot Reload
        configMonitor.OnChange(newConfig =>
        {
            Log.Information("Downloader configuration changed. Reloading downloaders...");
            lock (_downloaders)
            {
                _config = newConfig;
                _downloaders.Clear();
                InitializeDownloaders();
            }
        });
    }

    private void InitializeDownloaders()
    {
        foreach (var downloaderDef in _config.Downloaders)
        {
            if (!downloaderDef.Enabled) continue;

            // 5. Здесь мы решаем, какой класс создать, на основе имени в конфиге
            IMediaDownloader? downloader = downloaderDef.Name switch
            {
                "YtDlp" => new YtDlpDownloader(downloaderDef, _processRunner, _argumentBuilder),
                "GalleryDl" => new GalleryDlDownloader(downloaderDef, _processRunner, _argumentBuilder),
                // Можно будет легко добавить новый:
                // "NewCoolDownloader" => new NewCoolDownloader(downloaderDef, ...),
                _ => null
            };

            if (downloader != null)
            {
                _downloaders.Add(downloader);
                Log.Debug("Initialized downloader: {DownloaderName}", downloader.Name);
            }
        }
    }
    
    // Старые методы интерфейса теперь работают с нашим кешированным списком _downloaders
    public IMediaDownloader GetDownloader(string url)
    {
        var downloader = _downloaders
            .Where(d => d.CanHandle(url))
            .OrderByDescending(d => d.Priority)
            .FirstOrDefault();
            
        if (downloader == null)
        {
            throw new InvalidOperationException($"No enabled downloader found for URL: {url}");
        }
        
        Log.Information("Selected downloader {Downloader} for {Url}", downloader.Name, url);
        return downloader;
    }
    
    public IEnumerable<IMediaDownloader> GetDownloadersForUrl(string url)
    {
        return _downloaders
            .Where(d => d.CanHandle(url))
            .OrderByDescending(d => d.Priority);
    }
    
    public IEnumerable<IMediaDownloader> GetAllDownloaders() => _downloaders;
    public IEnumerable<IMediaDownloader> GetEnabledDownloaders() => _downloaders;
}