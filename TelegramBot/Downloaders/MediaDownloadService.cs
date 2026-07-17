// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

namespace TelegramMediaRelayBot.TelegramBot.Downloaders;

/// <summary>Downloaded files plus the temp directory that holds them; disposing deletes the directory.</summary>
public sealed class MediaDownloadResult : IDisposable
{
    public IReadOnlyList<MediaFile> Files { get; }
    private readonly string _workDir;

    public MediaDownloadResult(string workDir, IReadOnlyList<MediaFile> files)
    {
        _workDir = workDir;
        Files = files;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_workDir))
                Directory.Delete(_workDir, recursive: true);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to delete temp dir {Dir}", _workDir);
        }
    }
}

/// <summary>
/// Runs the download backends as an ordered fallback chain, resolving the proxy
/// per host, and hands back the files in a self-cleaning temp directory.
/// </summary>
public sealed class MediaDownloadService
{
    private static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "tmrb");

    private readonly IReadOnlyList<IMediaDownloader> _downloaders;
    private readonly AppConfig _config;

    public MediaDownloadService(IEnumerable<IMediaDownloader> downloaders, AppConfig config)
    {
        _downloaders = downloaders.OrderBy(d => d.Priority).ToList();
        _config = config;
    }

    public async Task<MediaDownloadResult> DownloadAsync(string url, IProgress<string>? progress, CancellationToken ct)
    {
        string workDir = Path.Combine(TempRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var uri = new Uri(url);
            string? proxy = _config.ResolveDownloadProxyUrl(uri.Host);

            foreach (var downloader in _downloaders.Where(d => d.CanHandle(uri)))
            {
                try
                {
                    var files = await downloader.DownloadAsync(uri, workDir, proxy, progress, ct);
                    if (files.Count > 0)
                        return new MediaDownloadResult(workDir, files);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{Backend} failed for {Url}", downloader.Name, url);
                }
            }

            return new MediaDownloadResult(workDir, Array.Empty<MediaFile>());
        }
        catch
        {
            TryDelete(workDir);
            throw;
        }
    }

    /// <summary>Deletes temp directories left behind by a previous crash. Call once at startup.</summary>
    public static void SweepOrphans()
    {
        try
        {
            if (!Directory.Exists(TempRoot)) return;
            foreach (var dir in Directory.EnumerateDirectories(TempRoot))
                TryDelete(dir);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Temp sweep failed");
        }
    }

    private static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
        catch (Exception ex) { Log.Debug(ex, "Failed to delete {Dir}", dir); }
    }
}
