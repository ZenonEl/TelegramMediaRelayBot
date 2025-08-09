using System.Diagnostics;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.Infrastructure.MediaProcessing;

public interface IMediaProcessingService
{
    Task<List<byte[]>> ApplySizePolicyAsync(List<byte[]> mediaFiles, DownloadingConfiguration cfg, CancellationToken ct);
}

public class FfmpegService : IMediaProcessingService
{
    public async Task<List<byte[]>> ApplySizePolicyAsync(List<byte[]> mediaFiles, DownloadingConfiguration cfg, CancellationToken ct)
    {
        if (cfg.IfTooLarge == TooLargeHandling.Off)
            return mediaFiles;

        var limitBytes = (long)cfg.TargetUploadLimitMb * 1024L * 1024L;
        var processed = new List<byte[]>(capacity: mediaFiles.Count);

        foreach (var file in mediaFiles)
        {
            Log.Debug("SizePolicy: start; original={SizeMB:F1}MB, limit={LimitMB}MB, policy={Policy}",
                file.LongLength / (1024.0 * 1024.0), cfg.TargetUploadLimitMb, cfg.IfTooLarge);
            if (file.LongLength <= limitBytes)
            {
                processed.Add(file);
                continue;
            }

            var transcoded = await TryTranscodeAsync(file, cfg, ct);
            if (transcoded != null && transcoded.LongLength <= limitBytes)
            {
                processed.Add(transcoded);
                continue;
            }

            if (cfg.IfTooLarge == TooLargeHandling.Split || cfg.IfTooLarge == TooLargeHandling.TranscodeThenSplit)
            {
                var parts = await TrySplitAsync(transcoded ?? file, cfg, ct);
                if (parts.Count > 0)
                {
                    processed.AddRange(parts);
                    continue;
                }
            }

            // Fallback: keep original if processing failed
            processed.Add(transcoded ?? file);
        }

        return processed;
    }

    private async Task<byte[]?> TryTranscodeAsync(byte[] inputBytes, DownloadingConfiguration cfg, CancellationToken ct)
    {
        if (cfg.IfTooLarge == TooLargeHandling.Split) return null;

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inPath = Path.Combine(tempDir, "input.mp4");
        var outPath = Path.Combine(tempDir, "output.mp4");
        try
        {
            await System.IO.File.WriteAllBytesAsync(inPath, inputBytes, ct);

            // compute adaptive bitrate to aim under TargetUploadLimitMb
            var durationSec = await TryGetDurationSecondsAsync(inPath, ct);
            var limitBytes = Math.Max(1, cfg.TargetUploadLimitMb) * 1024L * 1024L;
            var targetBytes = (long)(limitBytes * 0.95); // headroom

            var p = cfg.TranscodeProfile ?? new TranscodeProfileConfiguration();
            var audioKbps = Math.Max(64, p.AudioBitrateKbps);
            int videoKbps = p.VideoBitrateKbps;
            if (durationSec > 0)
            {
                var totalKbps = (int)Math.Max(300, (targetBytes * 8.0 / durationSec) / 1000.0);
                videoKbps = Math.Max(300, totalKbps - audioKbps);
            }

            var args = BuildTranscodeArgs(inPath, outPath, cfg, videoKbps, audioKbps);
            var ok = await RunProcessAsync("ffmpeg", args, TimeSpan.FromMinutes(12), ct);
            var exists = System.IO.File.Exists(outPath);
            if (!ok && !exists)
            {
                Log.Debug("SizePolicy: transcode failed (ok={Ok}, exists={Exists})", ok, exists);
                return null;
            }
            if (!ok && exists)
            {
                Log.Debug("SizePolicy: transcode exit!=0, but output exists. Using output");
            }
            var bytes = await System.IO.File.ReadAllBytesAsync(outPath, ct);
            Log.Debug("SizePolicy: transcoded size={SizeMB:F1}MB", bytes.LongLength / (1024.0 * 1024.0));

            // if still bigger than limit, tighten bitrate and retry once
            if (bytes.LongLength > limitBytes && durationSec > 0)
            {
                videoKbps = (int)(videoKbps * 0.8);
                var args2 = BuildTranscodeArgs(inPath, outPath, cfg, videoKbps, audioKbps);
                ok = await RunProcessAsync("ffmpeg", args2, TimeSpan.FromMinutes(12), ct);
                exists = System.IO.File.Exists(outPath);
                if (!ok && !exists)
                {
                    Log.Debug("SizePolicy: retry transcode failed (ok={Ok}, exists={Exists})", ok, exists);
                    return null;
                }
                if (!ok && exists)
                {
                    Log.Debug("SizePolicy: retry transcode exit!=0, but output exists. Using output");
                }
                bytes = await System.IO.File.ReadAllBytesAsync(outPath, ct);
                Log.Debug("SizePolicy: retry transcoded size={SizeMB:F1}MB", bytes.LongLength / (1024.0 * 1024.0));
            }

            return bytes;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "FFmpeg transcode failed");
            return null;
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private async Task<List<byte[]>> TrySplitAsync(byte[] inputBytes, DownloadingConfiguration cfg, CancellationToken ct)
    {
        var parts = new List<byte[]>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inPath = Path.Combine(tempDir, "input.mp4");
        try
        {
            await System.IO.File.WriteAllBytesAsync(inPath, inputBytes, ct);

            // Estimate target segment time from desired number of parts
            var targetBytes = Math.Max(1, cfg.TargetPartSizeMb) * 1024L * 1024L;
            var desiredParts = (int)Math.Ceiling(inputBytes.LongLength / (double)targetBytes);
            desiredParts = Math.Max(2, desiredParts);

            var duration = await TryGetDurationSecondsAsync(inPath, ct);
            var segTime = duration > 0
                ? Math.Max(5, (int)Math.Ceiling(duration / desiredParts))
                : 30; // fallback
            var outPattern = Path.Combine(tempDir, "part_%03d.mp4");
            // Re-encode with GOP and disabled scene change threshold, reset timestamps for reliable splitting
            var args = $"-y -hide_banner -loglevel error -i \"{inPath}\" -c:v libx264 -preset veryfast -crf 23 -g 60 -sc_threshold 0 -c:a aac -movflags +faststart -f segment -segment_time {segTime} -reset_timestamps 1 \"{outPattern}\"";

            var ok = await RunProcessAsync("ffmpeg", args, TimeSpan.FromMinutes(15), ct);
            var files = System.IO.Directory.GetFiles(tempDir, "part_*.mp4").OrderBy(f => f).ToArray();
            if (!ok && files.Length == 0)
            {
                Log.Debug("ffmpeg segmenting returned non-zero exit and no parts produced");
                return parts;
            }
            // Fallback: if got only one part, try halving segment time once
            if (files.Length <= 1 && duration > 0)
            {
                var segTime2 = Math.Max(5, segTime / 2);
                outPattern = Path.Combine(tempDir, "part2_%03d.mp4");
                var args2 = $"-y -hide_banner -loglevel error -i \"{inPath}\" -c:v libx264 -preset veryfast -crf 23 -g 60 -sc_threshold 0 -c:a aac -movflags +faststart -f segment -segment_time {segTime2} -reset_timestamps 1 \"{outPattern}\"";
                var ok2 = await RunProcessAsync("ffmpeg", args2, TimeSpan.FromMinutes(15), ct);
                files = System.IO.Directory.GetFiles(tempDir, "part2_*.mp4").OrderBy(f => f).ToArray();
                if (!ok2 && files.Length == 0)
                {
                    Log.Debug("ffmpeg segmenting retry returned non-zero exit and no parts produced");
                    return parts;
                }
            }
            foreach (var f in files)
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(f, ct);
                parts.Add(bytes);
            }
            return parts;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "FFmpeg split failed");
            return parts;
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private static async Task<double> TryGetDurationSecondsAsync(string inputPath, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = new Process { StartInfo = psi };
            p.Start();
            var output = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            if (double.TryParse(output.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                return seconds;
        }
        catch { }
        return 0;
    }

    private static string BuildTranscodeArgs(string inPath, string outPath, DownloadingConfiguration cfg, int videoBitrateKbps, int audioBitrateKbps)
    {
        var p = cfg.TranscodeProfile ?? new TranscodeProfileConfiguration();
        // Basic scaling filter from MaxResolution (e.g., 720p)
        var scaleFilter = p.MaxResolution.ToLowerInvariant() switch
        {
            "1080p" => "-vf scale=-2:1080",
            "720p" => "-vf scale=-2:720",
            "480p" => "-vf scale=-2:480",
            "360p" => "-vf scale=-2:360",
            _ => string.Empty
        };

        var vBitrate = videoBitrateKbps > 0 ? $"-b:v {videoBitrateKbps}k -maxrate {videoBitrateKbps}k -bufsize {videoBitrateKbps * 2}k" : "";
        var aBitrate = audioBitrateKbps > 0 ? $"-b:a {audioBitrateKbps}k" : "";
        var preset = !string.IsNullOrWhiteSpace(p.Preset) ? $"-preset {p.Preset}" : "-preset veryfast";

        var args = $"-y -hide_banner -loglevel error -i \"{inPath}\" {scaleFilter} -c:v libx264 {vBitrate} {preset} -c:a aac {aBitrate} -movflags +faststart \"{outPath}\"";
        return args.Replace("  ", " ").Trim();
    }

    private static async Task<bool> RunProcessAsync(string fileName, string arguments, TimeSpan timeout, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        var exited = await Task.WhenAny(process.WaitForExitAsync(ct), Task.Delay(timeout, ct)) == process.WaitForExitAsync(ct);
        if (!exited)
        {
            try { process.Kill(); } catch { }
            return false;
        }
        var err = await process.StandardError.ReadToEndAsync(ct);
        if (!string.IsNullOrWhiteSpace(err))
        {
            var tail = err.Length > 1000 ? err[^1000..] : err;
            Log.Debug("ffmpeg stderr(tail): {Err}", tail);
        }
        return process.ExitCode == 0;
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }
}

