// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace TelegramMediaRelayBot.TelegramBot.Downloaders;

/// <summary>
/// Primary backend. Forces an MP4/H.264/AAC selection so Telegram plays the
/// video inline with sound instead of dropping audio or falling back to a document.
/// </summary>
public sealed class YtDlpDownloader : IMediaDownloader
{
    private readonly AppConfig _config;

    public YtDlpDownloader(AppConfig config) => _config = config;

    public string Name => "yt-dlp";
    public int Priority => 10;
    public bool CanHandle(Uri url) => true;

    public async Task<IReadOnlyList<MediaFile>> DownloadAsync(
        Uri url, string workDir, string? proxyUrl, IProgress<string>? progress, CancellationToken ct)
    {
        var ytdl = new YoutubeDL
        {
            YoutubeDLPath = "yt-dlp",
            OutputFolder = workDir,
            OutputFileTemplate = "video.%(ext)s",
            OverwriteFiles = true
        };

        var options = new OptionSet
        {
            IgnoreConfig = true,
            NoCheckCertificates = true,
            // Telegram's inline player wants MP4 + H.264 + AAC; without an explicit
            // format yt-dlp can pick a video-only stream or merge into webm/mkv,
            // which plays silently or falls back to a document.
            Format = "bv*+ba/b",
            FormatSort = "vcodec:h264,lang,quality,res,fps,hdr:12,acodec:aac",
            MergeOutputFormat = DownloadMergeFormat.Mp4,
            RemuxVideo = "mp4",
            PostprocessorArgs = "ffmpeg:-movflags +faststart",
            NoPlaylist = true
        };

        if (!string.IsNullOrEmpty(proxyUrl))
            options.Proxy = proxyUrl;
        if (!string.IsNullOrEmpty(_config.Download.CookiesFromBrowser))
            options.CookiesFromBrowser = _config.Download.CookiesFromBrowser;
        if (!string.IsNullOrEmpty(_config.Download.CookiesFile))
            options.Cookies = _config.Download.CookiesFile;

        Log.Debug("yt-dlp: url={Url}, proxy={Proxy}", url, proxyUrl ?? "(direct)");

        var ytProgress = BuildProgressReporter(progress);
        var result = await ytdl.RunVideoDownload(url.ToString(), ct: ct, progress: ytProgress, overrideOptions: options);

        if (!result.Success)
        {
            Log.Error("yt-dlp failed for {Url}: {Errors}", url, string.Join("\n", result.ErrorOutput));
            return Array.Empty<MediaFile>();
        }

        string filePath = result.Data;
        if (!File.Exists(filePath))
        {
            Log.Error("yt-dlp reported success but file is missing: {Path}", filePath);
            return Array.Empty<MediaFile>();
        }

        return new[] { new MediaFile(filePath, MediaKindDetector.FromPath(filePath)) };
    }

    private IProgress<DownloadProgress>? BuildProgressReporter(IProgress<string>? progress)
    {
        if (progress is null) return null;

        string last = "";
        return new Progress<DownloadProgress>(p =>
        {
            int percent = (int)(p.Progress * 100);
            string text = $"[download] {percent}%";
            if (!string.IsNullOrEmpty(p.DownloadSpeed)) text += $" at {p.DownloadSpeed}";
            if (!string.IsNullOrEmpty(p.ETA)) text += $" ETA {p.ETA}";
            if (text == last) return;
            last = text;
            progress.Report(text);
        });
    }
}
