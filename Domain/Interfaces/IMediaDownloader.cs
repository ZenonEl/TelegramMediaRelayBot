// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace TelegramMediaRelayBot.Domain.Interfaces;

public interface IMediaDownloader
{
    string Name { get; }
    int Priority { get; }
    MediaType SupportedMediaTypes { get; }
    bool IsEnabled { get; }
    
    bool CanHandle(string url);
    Task<DownloadCapability> CheckCapabilityAsync(string url, CancellationToken ct);
    Task<DownloadResult> DownloadAsync(string url, DownloadOptions options, CancellationToken ct);
}

public interface IMediaDownloaderFactory
{
    IMediaDownloader GetDownloader(string url);
    IEnumerable<IMediaDownloader> GetDownloadersForUrl(string url);
    IEnumerable<IMediaDownloader> GetAllDownloaders();
    IEnumerable<IMediaDownloader> GetEnabledDownloaders();
}

public interface IDownloaderConfiguration
{
    string DownloaderName { get; }
    IConfigurationSection GetConfiguration();
    bool ValidateConfiguration(out List<string> errors);
}

public interface IUserPreferencesService
{
    Task<UserPreferences> GetUserPreferencesAsync(int userId, CancellationToken ct = default);
    Task UpdateUserPreferencesAsync(int userId, UserPreferences preferences, CancellationToken ct = default);
    Task TrackDownloadResultAsync(int userId, string url, string downloaderName, bool success, TimeSpan duration, long? fileSize, MediaType mediaType, CancellationToken ct = default);
} 