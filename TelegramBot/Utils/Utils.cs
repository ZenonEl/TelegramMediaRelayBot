// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.RegularExpressions;


namespace TelegramMediaRelayBot.TelegramBot.Utils ;

public static class CommonUtilities
{

    public static CancellationToken cancellationToken = MediaTelegramBot.TelegramBot.cancellationToken;
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
        text ??= Config.GetResourceString("ChooseOptionText");

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
        await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
        await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        MediaTelegramBot.TelegramBot.userStates.Remove(chatId);
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
            MediaTelegramBot.TelegramBot.userStates.Remove(chatId);
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

    public static FileType DetermineFileType(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
            return FileType.Document;

        string start = BitConverter.ToString(bytes.Take(4).ToArray()).Replace("-", "");
        
        var patterns = new Dictionary<FileType, string>
        {
            { FileType.Video, @"^(424242|00000020|52494646|57415645)" }, // H264, MP4, AVI, WAV
            { FileType.Photo, @"^(FFD8FF|89504E47|424D|49492A|4D4D2A)" }, // JPEG, PNG, BMP, TIFF
            { FileType.Audio, @"^(49443304)" } // MP3
        };

        foreach (var pattern in patterns)
        {
            Log.Verbose($"File bytes start: {start}");
            if (Regex.IsMatch(start, pattern.Value, RegexOptions.IgnoreCase))
                return pattern.Key;
        }

        return FileType.Document;
    }

    public static IEnumerable<IAlbumInputMedia> CreateMediaGroup(List<byte[]> files)
    {
        return files.Select(CreateMedia);
    }

    private static IAlbumInputMedia CreateMedia(byte[] file)
    {
        var fileType = DetermineFileType(file);
        switch (fileType)
        {
            case FileType.Photo:
                return new InputMediaPhoto(new MemoryStream(file));
            case FileType.Video:
                return new InputMediaVideo(new MemoryStream(file));
            case FileType.Audio:
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