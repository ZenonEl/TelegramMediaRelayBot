using System.Globalization;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Pipeline;
using TelegramMediaRelayBot.Infrastructure.Processes;

namespace TelegramMediaRelayBot.Infrastructure.Pipeline.PostProcessors;

public class VideoSplitterPostProcessor : IPostProcessor
{
    private readonly IProcessRunner _processRunner;
    private readonly SplitterConfig _config;
    private readonly string _ffmpegPath;

    public string Name => "VideoSplitter";
    public int Order => 5; 

    public VideoSplitterPostProcessor(
        IProcessRunner processRunner,
        IOptionsMonitor<DownloaderConfigRoot> configMonitor)
    {
        _processRunner = processRunner;
        _config = configMonitor.CurrentValue.MediaProcessing.Splitter;
        _ffmpegPath = configMonitor.CurrentValue.MediaProcessing.Ffmpeg.ExecutablePath;
    }

    public bool CanProcess(DownloadContext context)
    {
        if (!_config.Enabled) return false;
        
        return context.ResultFiles.Any(f => 
            f.MediaType == MediaType.Video && f.FileSize > _config.ThresholdBytes);
    }

    public async Task ProcessAsync(DownloadContext context, CancellationToken ct)
    {
        var newFilesList = new List<DownloadedFile>();

        foreach (var file in context.ResultFiles)
        {
            if (file.MediaType != MediaType.Video || file.FileSize <= _config.ThresholdBytes)
            {
                newFilesList.Add(file);
                continue;
            }

            context.ProgressCallback?.Invoke($"✂️ Файл большой ({file.FileSize / 1024 / 1024} MB). Нарезаю на части..."); //TODO жесткий перевод
            context.Log($"Splitting file {file.FileName} ({file.FileSize} bytes)...");

            try
            {
                double duration = await GetVideoDurationAsync(file.FilePath, ct);
                if (duration <= 0)
                {
                    context.Log("Failed to get duration. Skipping split.");
                    newFilesList.Add(file);
                    continue;
                }

                int partsCount = (int)Math.Ceiling((double)file.FileSize / _config.ChunkSizeBytes);
                double segmentDuration = duration / partsCount;

                context.Log($"Duration: {duration}s. Parts: {partsCount}. Segment: {segmentDuration}s.");

                List<DownloadedFile> chunks = await SplitFileAsync(file.FilePath, partsCount, segmentDuration, context, ct);
                
                if (chunks.Any())
                {
                    try { System.IO.File.Delete(file.FilePath); } catch { }
                    newFilesList.AddRange(chunks);
                }
                else
                {
                    newFilesList.Add(file);
                }
            }
            catch (Exception ex)
            {
                context.Log($"Error during splitting: {ex.Message}");
                newFilesList.Add(file);
            }
        }

        context.ResultFiles = newFilesList;
    }

    private async Task<double> GetVideoDurationAsync(string filePath, CancellationToken ct)
    {
        List<string> args = new List<string>
        {
            "-v", "error",
            "-show_entries", "format=duration",
            "-of", "default=noprint_wrappers=1:nokey=1",
            filePath
        };

        string ffprobeExec = "ffprobe"; //TODO улучшить в будущем поиск
        if (Path.IsPathRooted(_ffmpegPath))
        {
                string? dir = Path.GetDirectoryName(_ffmpegPath);
                if (dir != null) ffprobeExec = Path.Combine(dir, "ffprobe");
        }

        var result = await _processRunner.RunAsync(new ProcessRunOptions
        {
            FileName = ffprobeExec,
            Arguments = args,
            Timeout = TimeSpan.FromSeconds(10)
        }, ct);

        if (result.ExitCode == 0 && double.TryParse(result.Output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double duration))
        {
            return duration;
        }

        return 0;
    }

    private async Task<List<DownloadedFile>> SplitFileAsync(string inputFile, int parts, double segmentDuration, DownloadContext context, CancellationToken ct)
    {
        var outputFiles = new List<DownloadedFile>();
        string dir = Path.GetDirectoryName(inputFile)!;
        string name = Path.GetFileNameWithoutExtension(inputFile);
        string ext = Path.GetExtension(inputFile);

        for (int i = 0; i < parts; i++)
        {
            string outputName = $"{name}_part{i + 1:00}{ext}";
            string outputPath = Path.Combine(dir, outputName);
            
            double startTime = i * segmentDuration;

            var args = new List<string>
            {
                "-y",
                "-ss", startTime.ToString(CultureInfo.InvariantCulture),
                "-i", inputFile,
                "-t", segmentDuration.ToString(CultureInfo.InvariantCulture),
                "-c", "copy",
                "-map", "0",
                outputPath
            };

            context.Log($"Creating part {i+1}/{parts}...");
            
            var result = await _processRunner.RunAsync(new ProcessRunOptions
            {
                FileName = _ffmpegPath,
                Arguments = args,
                Timeout = TimeSpan.FromMinutes(5)
            }, ct);

            if (result.ExitCode == 0 && System.IO.File.Exists(outputPath))
            {
                outputFiles.Add(new DownloadedFile 
                { 
                    FilePath = outputPath, 
                    MediaType = MediaType.Video 
                });
            }
            else
            {
                context.Log($"Failed to create part {i+1}. Aborting split.");
                return new List<DownloadedFile>();
            }
        }

        return outputFiles;
    }
}