using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Pipeline;
using TelegramMediaRelayBot.Infrastructure.Processes;
using TelegramMediaRelayBot.Infrastructure.Services;

namespace TelegramMediaRelayBot.Infrastructure.Pipeline.PostProcessors;

public class FfmpegPostProcessor : IPostProcessor
{
    private readonly IProcessRunner _processRunner;
    private readonly IConcurrencyLimiter _limiter;
    private readonly FfmpegConfig _config;

    private const long SafeSizeLimit = 50331648; 

    public string Name => "FfmpegConverter";
    public int Order => 10;

    public FfmpegPostProcessor(
        IProcessRunner processRunner, 
        IConcurrencyLimiter limiter,
        IOptionsMonitor<DownloaderConfigRoot> configMonitor)
    {
        _processRunner = processRunner;
        _limiter = limiter;
        _config = configMonitor.CurrentValue.MediaProcessing.Ffmpeg;
    }

    public bool CanProcess(DownloadContext context)
    {
        return context.ResultFiles.Any(f => f.MediaType == MediaType.Video);
    }

    public async Task ProcessAsync(DownloadContext context, CancellationToken ct)
    {
        var newFilesList = new List<DownloadedFile>();
        
        int totalVideos = context.ResultFiles.Count(f => f.MediaType == MediaType.Video);
        int currentVideoIndex = 0;

        foreach (var file in context.ResultFiles)
        {
            if (file.MediaType != MediaType.Video)
            {
                newFilesList.Add(file);
                continue;
            }

            currentVideoIndex++;
            string inputPath = file.FilePath;
            long originalSize = new FileInfo(inputPath).Length;
            string fileName = Path.GetFileName(inputPath);

            bool isMp4 = Path.GetExtension(inputPath).Equals(".mp4", StringComparison.OrdinalIgnoreCase);
            
            if (isMp4 && originalSize < SafeSizeLimit)
            {
                context.Log($"[Skip Optimization] File '{fileName}' is MP4 and small enough ({originalSize / 1024 / 1024} MB).");
                newFilesList.Add(file);
                continue;
            }

            string statusMsg = totalVideos > 1 
                ? $"⚙️ Оптимизирую видео {currentVideoIndex} из {totalVideos}..." 
                : "⚙️ Оптимизирую видео...";
            context.ProgressCallback?.Invoke(statusMsg);

            string outputPath = Path.Combine(
                Path.GetDirectoryName(inputPath)!, 
                Path.GetFileNameWithoutExtension(inputPath) + "_opt.mp4"
            );

            context.Log($"Waiting for slot... File: {fileName} ({originalSize / 1024 / 1024} MB)");

            using (await _limiter.AcquireAsync(ct))
            {
                context.Log($"[FFmpeg Start] Converting {fileName}...");
                bool success = await RunFfmpegAsync(inputPath, outputPath, context, ct);

                if (success)
                {
                    long newSize = new FileInfo(outputPath).Length;
                    context.Log($"[FFmpeg Finish] Success. {originalSize/1024/1024} MB -> {newSize/1024/1024} MB");
                    try { System.IO.File.Delete(inputPath); } catch { }
                    newFilesList.Add(new DownloadedFile
                    {
                        FilePath = outputPath,
                        MediaType = MediaType.Video
                    });
                }
                else
                {
                    context.Log("[FFmpeg Fail] Conversion failed. Keeping original.");
                    newFilesList.Add(file);
                }
            }
        }

        context.ResultFiles = newFilesList;
    }

    private async Task<bool> RunFfmpegAsync(string input, string output, DownloadContext context, CancellationToken ct)
    {
        var args = new List<string>
        {
            "-y",
            "-i", input,
            "-c:v", "libx264",
            "-preset", _config.Preset,
            "-crf", _config.Crf.ToString(),
            "-c:a", "aac",
            "-b:a", "128k",
            "-movflags", "+faststart",
            output
        };

        var options = new ProcessRunOptions
        {
            FileName = _config.ExecutablePath,
            Arguments = args,
            Timeout = _config.OperationTimeout,
            // Можно добавить логирование stderr, если нужно видеть детали ошибки
            OnOutputLine = null 
        };

        var result = await _processRunner.RunAsync(options, ct);
        
        if (result.ExitCode != 0)
        {
            context.Log($"FFmpeg stderr: {result.ErrorOutput}");
            return false;
        }

        return true;
    }
}