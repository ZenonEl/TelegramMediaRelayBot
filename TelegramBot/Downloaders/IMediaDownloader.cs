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

public enum MediaKind { Video, Photo, Audio, Document }

/// <summary>A downloaded file on disk, ready to be streamed to Telegram.</summary>
public sealed record MediaFile(string Path, MediaKind Kind);

/// <summary>
/// A media-download backend (yt-dlp, gallery-dl, ...). Adding a backend means
/// adding one implementation; the orchestrator and the rest of the bot are
/// unaffected.
/// </summary>
public interface IMediaDownloader
{
    string Name { get; }

    /// <summary>Lower runs first in the fallback chain.</summary>
    int Priority { get; }

    /// <summary>Whether this backend should be attempted for the given URL.</summary>
    bool CanHandle(Uri url);

    /// <summary>
    /// Downloads media into <paramref name="workDir"/>. Returns the files, or an
    /// empty list if this backend produced nothing (the chain then tries the next).
    /// </summary>
    Task<IReadOnlyList<MediaFile>> DownloadAsync(
        Uri url,
        string workDir,
        string? proxyUrl,
        IProgress<string>? progress,
        CancellationToken ct);
}

public static class MediaKindDetector
{
    private static readonly HashSet<string> VideoExt = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mkv", ".webm", ".mov", ".avi", ".m4v", ".gifv" };
    private static readonly HashSet<string> PhotoExt = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".heic" };
    private static readonly HashSet<string> AudioExt = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".m4a", ".opus", ".ogg", ".oga", ".flac", ".wav", ".aac" };

    public static MediaKind FromPath(string path)
    {
        string ext = System.IO.Path.GetExtension(path);
        if (VideoExt.Contains(ext)) return MediaKind.Video;
        if (PhotoExt.Contains(ext)) return MediaKind.Photo;
        if (AudioExt.Contains(ext)) return MediaKind.Audio;
        return MediaKind.Document;
    }
}
