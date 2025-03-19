// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Diagnostics;
using DotNetTor.SocksPort;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramMediaRelayBot
{
    public class VideoGet
    {
        private static readonly string YtDlpPath = Path.Combine(AppContext.BaseDirectory, "yt-dlp");
        private static readonly string[] ColonSpaceSeparator = [": "];

        public static async Task<List<byte[]>?> DownloadMedia(ITelegramBotClient botClient, string videoUrl, Message statusMessage, CancellationToken cancellationToken)
        {
            try
            {
                if (Config.isUseGalleryDl)
                {
                    Log.Debug("Starting video download via gallery-dl...");
                    List<byte[]>? galleryFiles = await TryDownloadWithGalleryDlAsync(videoUrl, botClient, statusMessage, cancellationToken);

                    if (galleryFiles?.Count > 0)
                    {
                        return galleryFiles;
                    }
                }

                Log.Debug("Starting video download via yt-dlp...");
                
                string tempDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirPath);
                
                using var httpClient = new HttpClient(new SocksPortHandler(Config.torSocksHost, socksPort: Config.torSocksPort));

                if (Config.torEnabled)
                {
                    var result = await httpClient.GetStringAsync("https://check.torproject.org/api/ip");
                    Log.Debug("Tor IP: " + result);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--proxy \"{Config.proxy}\" -v -f mp4 --output \"{tempDirPath}/video.%(ext)s\" {videoUrl}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    Log.Debug("Process started.");

                    List<string> outputLines = new List<string>();
                    List<string> errorLines = new List<string>();

                    Task readOutputTask = ReadLinesAsync(process.StandardOutput, outputLines, botClient, statusMessage, cancellationToken);
                    Task readErrorTask = ReadLinesAsync(process.StandardError, errorLines, botClient, statusMessage, cancellationToken);

                    await process.WaitForExitAsync();

                    await Task.WhenAll(readOutputTask, readErrorTask);

                    string output = string.Join("\n", outputLines);
                    string error = string.Join("\n", errorLines);

                    if (process.ExitCode == 0)
                    {
                        try
                        {
                            string? downloadLine = output.Split('\n').FirstOrDefault(line => line.StartsWith("[download] Destination:"));
                            if (downloadLine == null)
                            {
                                Log.Error("Could not find download destination in yt-dlp output.");
                                return null;
                            }

                            string[] parts = downloadLine.Split(ColonSpaceSeparator, 2, StringSplitOptions.None);
                            if (parts.Length < 2)
                            {
                                Log.Error("Download destination not found in yt-dlp output.");
                                return null;
                            }

                            string finalFilePath = parts[1].Trim();
                            Log.Debug($"Final file path: {finalFilePath}");

                            if (System.IO.File.Exists(finalFilePath))
                            {
                                List<byte[]>? videoBytes = new List<byte[]> { System.IO.File.ReadAllBytes(finalFilePath) };
                                
                                System.IO.File.Delete(finalFilePath);
                                Directory.Delete(tempDirPath, recursive: true);
                                
                                return videoBytes;
                            }
                            
                            Log.Error($"Final file does not exist: {finalFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error reading file: {ex.Message}");
                        }
                    }
                    else
                    {
                        Log.Error("Video download failed: " + error);
                    }
                    
                    Directory.Delete(tempDirPath, recursive: true);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in DownloadVideoAsync");
                return null;
            }
        }

        private static async Task<List<byte[]>?> TryDownloadWithGalleryDlAsync(string url, ITelegramBotClient botClient, Message statusMessage, CancellationToken cancellationToken)
        {
            string? tempDir = null;
            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                //TODO Добавить разные параметры настройки gallery в appsettings
                //TODO Не забыть добавить переключатель использования gallery-dl и всё окончательно затестить
                //TODO Улучшить в целом работу с параметрами yt и gl и их конфиг файлами
                var startInfo = new ProcessStartInfo
                {
                    FileName = "gallery-dl.bin",
                    Arguments = $"--proxy \"{Config.proxy}\" -d \"{tempDir}\" -D \"{tempDir}\" --verbose \"{url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    Log.Debug("Process started.");

                    List<string> outputLines = new List<string>();
                    List<string> errorLines = new List<string>();

                    Task readOutputTask = ReadLinesAsync(process.StandardOutput, outputLines, botClient, statusMessage, cancellationToken);
                    Task readErrorTask = ReadLinesAsync(process.StandardError, errorLines, botClient, statusMessage, cancellationToken);

                    await process.WaitForExitAsync();

                    await Task.WhenAll(readOutputTask, readErrorTask);

                    if (process.ExitCode == 0)
                    {
                        var files = Directory.GetFiles(tempDir);
                        var result = new List<byte[]>();
                        foreach (var file in files)
                        {
                            Log.Debug(file);
                            result.Add(await System.IO.File.ReadAllBytesAsync(file));
                        }

                        return result.Count > 0 ? result : null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Gallery-dl download error");
            }
            finally
            {
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            return null;
        }

        private static async Task ReadLinesAsync(StreamReader reader, List<string> lines, ITelegramBotClient botClient, Message statusMessage, CancellationToken cancellationToken)
        {
            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    if (line.Contains("[download]"))
                    {
                        try
                        {
                            if (!lines.Contains(line))
                            {
                                await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, 
                                RemoveUntilDownload(line),
                                cancellationToken: cancellationToken);
                                await Task.Delay(Config.videoGetDelay, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex, "Error editing message.");
                        }
                        if (Config.showVideoDownloadProgress) Log.Debug($"Video download progress: {line}");
                    }
                    lines.Add(line);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading output lines.");
            }
        }

        private static string RemoveUntilDownload(string line)
        {
            int startIndex = line.IndexOf("[download]");
            return startIndex != -1 ? line.Substring(startIndex) : line;
        }
    }
}