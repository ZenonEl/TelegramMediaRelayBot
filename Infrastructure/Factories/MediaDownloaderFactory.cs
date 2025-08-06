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


namespace TelegramMediaRelayBot.Infrastructure.Factories;

public class MediaDownloaderFactory : IMediaDownloaderFactory
{
    private readonly IEnumerable<IMediaDownloader> _downloaders;
    
    public MediaDownloaderFactory(IEnumerable<IMediaDownloader> downloaders)
    {
        _downloaders = downloaders;
    }
    
    public IMediaDownloader GetDownloader(string url)
    {
        var downloader = _downloaders
            .Where(d => d.IsEnabled && d.CanHandle(url))
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
            .Where(d => d.IsEnabled && d.CanHandle(url))
            .OrderByDescending(d => d.Priority);
    }
    
    public IEnumerable<IMediaDownloader> GetAllDownloaders() => _downloaders;
    public IEnumerable<IMediaDownloader> GetEnabledDownloaders() => _downloaders.Where(d => d.IsEnabled);
} 