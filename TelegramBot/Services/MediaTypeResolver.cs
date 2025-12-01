// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Telegram.Bot.Types;

using TelegramMediaRelayBot.TelegramBot.Utils; // Для IAlbumInputMedia и т.д.

namespace TelegramMediaRelayBot.TelegramBot.Services;

// Переименовываем MediaFileType, чтобы не конфликтовать с доменной моделью
public enum TelegramFileType { Photo, Video, Audio, Document }

public interface IMediaTypeResolver
{
    TelegramFileType DetermineFileType(byte[] bytes);
    IEnumerable<IAlbumInputMedia> CreateMediaGroup(List<byte[]> files);
}

public class MediaTypeResolver : IMediaTypeResolver
{
    public TelegramFileType DetermineFileType(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
            return TelegramFileType.Document;

        string start = BitConverter.ToString(bytes.Take(12).ToArray()).Replace("-", "");

        var patterns = new Dictionary<TelegramFileType, string>
        {
            // Video: MP4/ISO BMFF (ftyp), WebM(Matroska EBML 1A45DFA3), AVI(RIFF 52494646 + 415649), MKV(1A45DFA3)
            { TelegramFileType.Video, @"^(000000..66747970|1A45DFA3|52494646.{8}415649|57415645)" },
            // Photo: JPEG/PNG/GIF/BMP/TIFF/WebP (RIFF WEBP)
            { TelegramFileType.Photo, @"^(FFD8FF|89504E47|47494638|424D|49492A|4D4D2A|52494646.{8}57454250)" },
            // Audio: ID3(MP3), OGG(4F676753), FLAC(664C6143), WAV(RIFF WAVE)
            { TelegramFileType.Audio, @"^(494433|4F676753|664C6143|52494646.{8}57415645)" }
        };

        foreach (var pattern in patterns)
        {
            Log.Verbose($"File bytes start: {start}");
            if (Regex.IsMatch(start, pattern.Value, RegexOptions.IgnoreCase))
                return pattern.Key;
        }

        return TelegramFileType.Document;
    }

    public IEnumerable<IAlbumInputMedia> CreateMediaGroup(List<byte[]> files)
    {
        return files.Select(CreateMedia);
    }

    private IAlbumInputMedia CreateMedia(byte[] file)
    {
        TelegramFileType fileType = DetermineFileType(file);
        switch (fileType)
        {
            case TelegramFileType.Photo:
                return new InputMediaPhoto(new MemoryStream(file));
            case TelegramFileType.Video:
                return new InputMediaVideo(new MemoryStream(file));
            case TelegramFileType.Audio:
                return new InputMediaAudio(new MemoryStream(file));
            default:
                return new InputMediaDocument(new MemoryStream(file));
        }
    }
}
