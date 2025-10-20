using System.Text.RegularExpressions;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;
using TelegramMediaRelayBot.Infrastructure.Processes;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders;

public class YtDlpDownloader : BaseMediaDownloader
{
    // Конструктор просто передает зависимости в базовый класс
    public YtDlpDownloader(
        DownloaderDefinition config, 
        IProcessRunner processRunner, 
        IArgumentBuilder argumentBuilder) 
        : base(config, processRunner, argumentBuilder)
    {
    }

    protected override async Task<DownloadResult> ProcessSuccessResult(string tempDirPath, CommandResult commandResult, CancellationToken ct)
    {
        string[] files = Directory.GetFiles(tempDirPath);
        
        if (files.Length == 0)
        {
            return new DownloadResult { Success = false, ErrorMessage = "No files were downloaded by yt-dlp." };
        }

        Array.Reverse(files);

        List<byte[]> mediaFiles = [];
        long totalSize = 0;

        foreach (string file in files)
        {
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(file, ct);
            mediaFiles.Add(fileBytes);
            totalSize += fileBytes.Length;
        }

        return new DownloadResult
        {
            Success = true,
            MediaFiles = mediaFiles,
            MediaType = MediaType.Video, // TODO: Улучшить
            FileSize = totalSize
        };
    }
}