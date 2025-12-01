// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Models;

namespace TelegramMediaRelayBot.Domain.Interfaces;


public interface IMediaDownloader
{
    string Name { get; }
    int Priority { get; }
    bool IsEnabled { get; }
    DownloaderDefinition Config { get; }

    /// <summary>
    /// Проверяет, может ли этот загрузчик обработать URL на основе конфигурации.
    /// </summary>
    bool CanHandle(string url);

    /// <summary>
    /// Выполняет одну попытку скачивания.
    /// </summary>
    Task<DownloadResult> Download(string url, DownloadOptions options, CancellationToken ct);
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
