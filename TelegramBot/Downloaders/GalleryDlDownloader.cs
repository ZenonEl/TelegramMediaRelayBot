// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Diagnostics;

namespace TelegramMediaRelayBot.TelegramBot.Downloaders;

/// <summary>
/// Fallback backend for galleries and sites yt-dlp does not cover. Enabled via
/// Download.UseGalleryDl; runs after yt-dlp in the chain.
/// </summary>
public sealed class GalleryDlDownloader : IMediaDownloader
{
    private readonly AppConfig _config;

    public GalleryDlDownloader(AppConfig config) => _config = config;

    public string Name => "gallery-dl";
    public int Priority => 20;
    public bool CanHandle(Uri url) => _config.Download.UseGalleryDl;

    public async Task<IReadOnlyList<MediaFile>> DownloadAsync(
        Uri url, string workDir, string? proxyUrl, IProgress<string>? progress, CancellationToken ct)
    {
        try
        {
            return await RunAsync("gallery-dl", url, workDir, proxyUrl, progress, ct);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return await RunAsync("gallery-dl.bin", url, workDir, proxyUrl, progress, ct);
        }
    }

    private static async Task<IReadOnlyList<MediaFile>> RunAsync(
        string binary, Uri url, string workDir, string? proxyUrl, IProgress<string>? progress, CancellationToken ct)
    {
        var args = new List<string> { "--config-ignore", "-D", workDir, "--range", "1-20" };
        if (!string.IsNullOrEmpty(proxyUrl)) { args.Add("--proxy"); args.Add(proxyUrl); }
        args.Add(url.ToString());

        var startInfo = new ProcessStartInfo
        {
            FileName = binary,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) startInfo.ArgumentList.Add(a);

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        Log.Debug("gallery-dl started: {Binary}, proxy={Proxy}", binary, proxyUrl ?? "(direct)");

        var stdout = ReadLinesAsync(process.StandardOutput, progress, ct);
        var stderr = ReadLinesAsync(process.StandardError, null, ct);
        await process.WaitForExitAsync(ct);
        await Task.WhenAll(stdout, stderr);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"gallery-dl exited with code {process.ExitCode}");

        return Directory.GetFiles(workDir)
            .Select(p => new MediaFile(p, MediaKindDetector.FromPath(p)))
            .ToList();
    }

    private static async Task ReadLinesAsync(StreamReader reader, IProgress<string>? progress, CancellationToken ct)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (progress != null && line.Contains("[download]"))
                    progress.Report(line[line.IndexOf("[download]", StringComparison.Ordinal)..]);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "gallery-dl output read error");
        }
    }
}
