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
        // --- ИСПРАВЛЕНИЕ ---
        // 1. Получаем файлы не как строки, а как объекты FileInfo
        var fileInfos = new DirectoryInfo(tempDirPath).GetFiles();
        
        // 2. СОРТИРУЕМ их по времени создания, чтобы гарантировать правильный порядок
        var sortedFiles = fileInfos.OrderBy(f => f.CreationTimeUtc).ToList();
        
        if (sortedFiles.Count == 0)
        {
            return new DownloadResult { Success = false, ErrorMessage = "No files were downloaded." };
        }

        var mediaFiles = new List<byte[]>();
        long totalSize = 0;

        foreach (var fileInfo in sortedFiles)
        {
            // 3. Читаем файлы в правильном порядке
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fileInfo.FullName, ct);
            mediaFiles.Add(fileBytes);
            totalSize += fileBytes.Length;
        }

        return new DownloadResult
        {
            Success = true,
            MediaFiles = mediaFiles,
            MediaType = MediaType.Image,
            FileSize = totalSize
        };
    }
}