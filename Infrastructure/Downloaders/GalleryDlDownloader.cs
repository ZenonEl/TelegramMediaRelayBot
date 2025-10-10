using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;
using TelegramMediaRelayBot.Infrastructure.Processes;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders;

public class GalleryDlDownloader : BaseMediaDownloader
{
    public GalleryDlDownloader(
        DownloaderDefinition config, 
        IProcessRunner processRunner, 
        IArgumentBuilder argumentBuilder) 
        : base(config, processRunner, argumentBuilder)
    {
    }

    protected override async Task<DownloadResult> ProcessSuccessResult(string tempDirPath, CommandResult commandResult, CancellationToken ct)
    {
        // GalleryDl может скачать много файлов, собираем все.
        var files = Directory.GetFiles(tempDirPath);
        
        if (files.Length == 0)
        {
            return new DownloadResult { Success = false, ErrorMessage = "No files were downloaded." };
        }

        var mediaFiles = new List<byte[]>();
        long totalSize = 0;

        foreach (var file in files)
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(file, ct);
            mediaFiles.Add(fileBytes);
            totalSize += fileBytes.Length;
        }

        return new DownloadResult
        {
            Success = true,
            MediaFiles = mediaFiles,
            MediaType = MediaType.Image, // TODO: Улучшить определение типа
            FileSize = totalSize
        };
    }
}