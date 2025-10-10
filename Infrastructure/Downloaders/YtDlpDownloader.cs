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
        // YtDlp скачивает один файл, нам нужно его найти.
        
        // Сначала ищем путь в логах
        var filePath = ExtractFilePath(commandResult.Output);

        // Если не нашли, ищем самый большой медиа-файл в папке
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            var bestFile = new DirectoryInfo(tempDirPath)
                .GetFiles()
                .OrderByDescending(f => f.Length)
                .FirstOrDefault();
            
            filePath = bestFile?.FullName;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            return new DownloadResult { Success = false, ErrorMessage = "Could not find downloaded file." };
        }
        
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);

        return new DownloadResult
        {
            Success = true,
            MediaFiles = new List<byte[]> { fileBytes },
            MediaType = MediaType.Video, // TODO: Улучшить определение типа
            FileSize = fileBytes.Length
        };
    }

    private string? ExtractFilePath(string output)
    {
        // TODO: Взять паттерн из конфига
        var match = Regex.Match(output, "\\[download\\] Destination: (.+)");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}