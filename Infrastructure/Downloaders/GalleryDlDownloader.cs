// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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

        Array.Reverse(files);

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