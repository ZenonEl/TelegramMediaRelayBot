using System.Diagnostics;
using Serilog;

namespace TikTokMediaRelayBot
{
    public class VideoGet
    {
        private static readonly string YtDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "yt-dlp");
        private static readonly string Proxy = "socks5://127.0.0.1:9150";

        public static async Task<byte[]?> DownloadVideoAsync(string videoUrl)
        {
            try
            {
                Log.Debug("Starting video download.");
                Log.Debug($"Yt-dlp path: {YtDlpPath}");

                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
                Log.Debug($"Temporary file path: {tempFilePath}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = YtDlpPath,
                    Arguments = $"--proxy \"{Proxy}\" -v -f bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best --output {tempFilePath} {videoUrl}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Log.Debug($"Arguments: {startInfo.Arguments}");

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    Log.Debug("Process started.");

                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();

                    await Task.WhenAll(outputTask, errorTask);

                    process.WaitForExit();

                    string output = await outputTask;
                    string error = await errorTask;

                    Log.Debug($"Output: {output}");
                    Log.Debug($"Error: {error}");

                    if (process.ExitCode == 0)
                    {
                        try
                        {
                            byte[] videoBytes = File.ReadAllBytes(tempFilePath);
                            Log.Debug("Download completed.");

                            File.Delete(tempFilePath);
                            Log.Debug("Temporary file deleted.");

                            return videoBytes;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error reading file: {ex.Message}");
                            return null;
                        }
                    }
                    else
                    {
                        Log.Error($"Ошибка при скачивании видео: {error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Произошла ошибка: {ex.Message}");
                return null;
            }
        }
    }
}