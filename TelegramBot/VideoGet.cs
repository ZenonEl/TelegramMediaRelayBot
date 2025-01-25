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
        private static readonly string Proxy = Config.proxy;
        private static readonly string[] ColonSpaceSeparator = [": "];

        public static async Task<byte[]?> DownloadVideoAsync(ITelegramBotClient botClient, string videoUrl, Message statusMessage, CancellationToken cancellationToken)
        {
            try
            {
                Log.Debug("Starting video download.");
                Log.Debug($"Yt-dlp path: {YtDlpPath}");

                string tempDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirPath);
                Log.Debug($"Temporary directory path: {tempDirPath}");
                using (var httpClient = new HttpClient(new SocksPortHandler(Config.torSocksHost, socksPort: Config.torSocksPort)))
                {
                    var result = await httpClient.GetStringAsync("https://check.torproject.org/api/ip");
                    Log.Debug("Tor IP: " + result);
                }
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = YtDlpPath,
                    Arguments = $"--proxy \"{Proxy}\" -v -f mp4 --output \"{tempDirPath}/video.%(ext)s\" {videoUrl}",
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
                            string downloadLine = output.Split('\n').FirstOrDefault(line => line.StartsWith("[download] Destination:"));
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
                                byte[] videoBytes = System.IO.File.ReadAllBytes(finalFilePath);
                                Log.Debug("Download completed.");

                                System.IO.File.Delete(finalFilePath);
                                Directory.Delete(tempDirPath, recursive: true);
                                Log.Debug("Temporary file and directory deleted.");

                                return videoBytes;
                            }
                            else
                            {
                                Log.Error($"Final file does not exist: {finalFilePath}");
                                return null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error reading file: {ex.Message}, {nameof(DownloadVideoAsync)}");
                            return null;
                        }
                    }
                    else
                    {
                        Log.Error(error, "Video download failed {MethodName}", nameof(DownloadVideoAsync));
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error: {ex.Message}, {nameof(DownloadVideoAsync)}");
                return null;
            }
        }

        private static async Task ReadLinesAsync(StreamReader reader, List<string> lines, ITelegramBotClient botClient, Message statusMessage, CancellationToken cancellationToken)
        {
            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    lines.Add(line);
                    if (line.Contains("[download]"))
                    {
                        try
                        {
                            await botClient.EditMessageText(statusMessage.Chat.Id, statusMessage.MessageId, 
                            line, 
                            cancellationToken: cancellationToken);
                            await Task.Delay(Config.videoGetDelay, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex, "Error editing message.");
                        }
                        if (Config.showVideoDownloadProgress) Log.Debug($"Video download progress: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading output lines.");
            }
        }
    }
}