using System.Diagnostics;


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
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = YtDlpPath,
                    Arguments = $"--proxy \"{Proxy}\" -f bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best --output - {videoUrl}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        await process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
                        byte[] videoBytes = memoryStream.ToArray();

                        if (process.ExitCode == 0)
                        {
                            return videoBytes;
                        }
                        else
                        {
                            string error = await process.StandardError.ReadToEndAsync();
                            Console.WriteLine($"Ошибка при скачивании видео: {error}");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                return null;
            }
        }
    }
}