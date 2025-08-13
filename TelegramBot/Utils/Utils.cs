// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.RegularExpressions;


namespace TelegramMediaRelayBot.TelegramBot.Utils;

public static class CommonUtilities
{
    private static readonly System.Resources.ResourceManager _resourceManager = 
        new System.Resources.ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    
    public static CancellationToken cancellationToken = TGBot.cancellationToken;
    
    public static string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }
    public static Task ErrorHandler(ITelegramBotClient _, Exception exception, CancellationToken __)
    {
        Log.Error($"Error occurred: {exception.Message}");
        Log.Error($"Stack trace: {exception.StackTrace}");

        if (exception.InnerException != null)
        {
            Log.Error($"Inner exception: {exception.InnerException.Message}");
            Log.Error($"Inner exception stack trace: {exception.InnerException.StackTrace}");
        }

        return Task.CompletedTask;
    }

    public static long GetIDfromUpdate(Update update)
    {
        if (update == null) return 0;
        if (update.Message != null)
        {
            return update.Message.Chat.Id;
        }
        else if (update.CallbackQuery != null)
        {
            return update.CallbackQuery.Message!.Chat.Id;
        }
        return 0;
    }

    public static bool CheckNonZeroID(long id)
    {
        if (id == 0) return true;
        return false;
    }

    public static bool CheckPrivateChatType(Update update)
    {
        if (update.Message != null && update.Message.Chat.Type == ChatType.Private) return true;
        if (update.CallbackQuery != null && update.CallbackQuery.Message!.Chat.Type == ChatType.Private) return true;
        return false;
    }

    public static Task SendMessage(ITelegramBotClient botClient, Update update, InlineKeyboardMarkup replyMarkup,
                                    CancellationToken cancellationToken, string? text = null)
    {
        text ??= GetResourceString("ChooseOptionText");

        long chatId = GetIDfromUpdate(update);

        if (update.CallbackQuery != null)
        {
            return botClient.EditMessageText(
                chatId: chatId,
                messageId: update.CallbackQuery.Message!.MessageId,
                text: text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Html
            );
        }

        if (update.Message != null)
        {
            return botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Html
            );
        }

        return Task.CompletedTask;
    }

    public static async Task AlertMessageAndShowMenu(ITelegramBotClient botClient, Update update, long chatId, string text)
    {
        await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken).ConfigureAwait(false);
        await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken).ConfigureAwait(false);
        TGBot.StateManager.Remove(chatId);
    }

    public static async Task<bool> HandleStateBreakCommand(ITelegramBotClient botClient,
                                                            Update update,
                                                            long chatId,
                                                            string command = "/start",
                                                            string callbackData = "main_menu",
                                                            bool removeReplyMarkup = true)
    {
        if (update.Message != null && update.Message.Text == command || 
            update.CallbackQuery != null && update.CallbackQuery.Data == callbackData)
        {
            if (removeReplyMarkup) await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
            TGBot.StateManager.Remove(chatId);
            return true;
        }
        return false;
    }

    public static bool IsLink(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Uri.TryCreate(input, UriKind.Absolute, out Uri? uriResult)
                            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public static bool TryExtractLinkAndText(string message, out string link, out string text)
    {
        link = string.Empty;
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(message)) return false;

        // 1) Ищем первый http(s) URL в любом месте строки
        var m = Regex.Match(message, @"https?://[^\s]+", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            link = m.Value.TrimEnd('.', ',', ';', '!', '?', ')', ']');
            // Подписью считаем ХВОСТ после первой ссылки; всё, что ДО ссылки — игнорируем
            int startAfterUrl = m.Index + m.Length;
            text = startAfterUrl < message.Length ? message[startAfterUrl..].Trim() : string.Empty;
            return true;
        }
        return false;
    }

    public static string ExtractDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return string.Empty;
        return uri.Host.ToLower();
    }

    public const int TelegramCaptionLimit = 1024;

    public static string SanitizeCaptionRemoveHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        string noTags = Regex.Replace(input, "<[^>]+>", string.Empty);
        noTags = noTags.Replace("\r", "").Replace("\u0000", "");
        return noTags.Trim();
    }

    public static string TrimCaptionToLimit(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= TelegramCaptionLimit ? text : text[..TelegramCaptionLimit];
    }

    public static string ParseStartCommand(string message)
    {
        string[] parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length > 0 && parts[0] == "/start")
        {
            if (parts.Length > 1 && Guid.TryParse(parts[1], out Guid link))
            {
                return link.ToString();
            }
            else
            {
                return "";
            }
        }
        else
        {
            return "";
        }
    }

    public static MediaFileType DetermineFileType(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
            return MediaFileType.Document;

        string start = BitConverter.ToString(bytes.Take(12).ToArray()).Replace("-", "");
        
        var patterns = new Dictionary<MediaFileType, string>
        {
            // Video: MP4/ISO BMFF (ftyp), WebM(Matroska EBML 1A45DFA3), AVI(RIFF 52494646 + 415649), MKV(1A45DFA3)
            { MediaFileType.Video, @"^(000000..66747970|1A45DFA3|52494646.{8}415649|57415645)" },
            // Photo: JPEG/PNG/GIF/BMP/TIFF/WebP (RIFF WEBP)
            { MediaFileType.Photo, @"^(FFD8FF|89504E47|47494638|424D|49492A|4D4D2A|52494646.{8}57454250)" },
            // Audio: ID3(MP3), OGG(4F676753), FLAC(664C6143), WAV(RIFF WAVE)
            { MediaFileType.Audio, @"^(494433|4F676753|664C6143|52494646.{8}57415645)" }
        };

        foreach (var pattern in patterns)
        {
            Log.Verbose($"File bytes start: {start}");
            if (Regex.IsMatch(start, pattern.Value, RegexOptions.IgnoreCase))
                return pattern.Key;
        }

        return MediaFileType.Document;
    }

    public static IEnumerable<IAlbumInputMedia> CreateMediaGroup(List<byte[]> files)
    {
        return files.Select(CreateMedia);
    }

    private static IAlbumInputMedia CreateMedia(byte[] file)
    {
        MediaFileType fileType = DetermineFileType(file);
        switch (fileType)
        {
            case MediaFileType.Photo:
                return new InputMediaPhoto(new MemoryStream(file));
            case MediaFileType.Video:
                return new InputMediaVideo(new MemoryStream(file));
            case MediaFileType.Audio:
                return new InputMediaAudio(new MemoryStream(file));
            default:
                return new InputMediaDocument(new MemoryStream(file));
        }
    }
}

public class ProgressReportingStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _totalBytes;
        private long _bytesRead;
        private readonly DateTimeOffset _startTime;

        public event Action<string>? OnProgress;

        public ProgressReportingStream(Stream baseStream)
        {
            _baseStream = baseStream;
            _totalBytes = _baseStream.Length;
            _startTime = DateTimeOffset.Now;
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _baseStream.Read(buffer, offset, count);
            _bytesRead += bytesRead;

            string progressMessage = GenerateProgressMessage();
            OnProgress?.Invoke(progressMessage);

            return bytesRead;
        }

        private string GenerateProgressMessage()
        {
            double percentage = _bytesRead * 100.0 / _totalBytes;
            TimeSpan elapsedTime = DateTimeOffset.Now - _startTime;

            double speed = elapsedTime.TotalSeconds > 0 ? _bytesRead / elapsedTime.TotalSeconds : 0;

            long remainingBytes = _totalBytes - _bytesRead;
            TimeSpan eta = speed > 0 ? TimeSpan.FromSeconds(remainingBytes / speed) : TimeSpan.Zero;

            string speedFormatted = FormatSpeed(speed);
            string etaFormatted = FormatTime(eta);

            return $"[Video Upload] {percentage:F1}% of {FormatSize(_totalBytes)} at {speedFormatted}/s ETA {etaFormatted}";
        }

        private static string FormatSpeed(double speed)
        {
            if (speed < 1024)
                return $"{speed:F2} B";
            else if (speed < 1024 * 1024)
                return $"{speed / 1024:F2} KiB";
            else
                return $"{speed / (1024 * 1024):F2} MiB";
        }

        private static string FormatSize(long size)
        {
            if (size < 1024)
                return $"{size} B";
            else if (size < 1024 * 1024)
                return $"{size / 1024} KiB";
            else
                return $"{size / (1024 * 1024)} MiB";
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalSeconds < 1)
                return "00:00";
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        public override void Flush() => _baseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        public override void SetLength(long value) => _baseStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
    }