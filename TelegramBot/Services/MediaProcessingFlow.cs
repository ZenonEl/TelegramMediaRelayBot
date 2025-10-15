using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Infrastructure.MediaProcessing;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;
using Telegram.Bot.Types.Enums;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IMediaProcessingFlow
{
    Task StartFlow(ITelegramBotClient botClient, DownloadSession session, List<long>? targetUserIds = null);
}

public class MediaProcessingFlow : IMediaProcessingFlow
{
    private readonly MediaDownloaderService _downloaderService;
    private readonly IMediaProcessingService _mediaProcessor;
    private readonly ITelegramSenderService _senderService; // Наш новый сервис отправки
    private readonly IOptionsMonitor<DownloadingConfiguration> _downloadingConfig;

    public MediaProcessingFlow(
        MediaDownloaderService downloaderService,
        IMediaProcessingService mediaProcessor,
        ITelegramSenderService senderService,
        IOptionsMonitor<DownloadingConfiguration> downloadingConfig)
    {
        _downloaderService = downloaderService;
        _mediaProcessor = mediaProcessor;
        _senderService = senderService;
        _downloadingConfig = downloadingConfig;
    }

    public async Task StartFlow(ITelegramBotClient botClient, DownloadSession session, List<long>? targetUserIds = null)
    {
        try
        {
            // --- ЭТАП 1: СКАЧИВАНИЕ ---
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Downloading...", cancellationToken: session.SessionCts.Token);
            
            await using var logUpdater = new TelegramLogUpdater(botClient, session.StatusMessageId, session.ChatId);
            logUpdater.Start();
            
            var options = new DownloadOptions { OnProgress = logUpdater.HandleLogLine };
            var downloadResult = await _downloaderService.DownloadMedia(session.Url, options, session.SessionCts.Token);

            if (!downloadResult.Success)
            {
                var errorMessage = $"Download failed:\n`{downloadResult.ErrorMessage}`";
                await botClient.EditMessageText(session.ChatId, session.StatusMessageId, errorMessage, parseMode: ParseMode.Markdown);
                return; // Завершаем флоу
            }

            // --- ЭТАП 2: ПОСТОБРАБОТКА ---
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Processing media...", cancellationToken: session.SessionCts.Token);

            var processedFiles = await _mediaProcessor.ApplySizePolicyAsync(downloadResult.MediaFiles, _downloadingConfig.CurrentValue, session.SessionCts.Token);

            // --- ЭТАП 3: ОТПРАВКА ---
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Sending media...", cancellationToken: session.SessionCts.Token);

            await _senderService.SendMedia(botClient, session, processedFiles, targetUserIds);

            // --- ЭТАП 4: ЗАВЕРШЕНИЕ ---
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Done!", cancellationToken: CancellationToken.None); // Используем CancellationToken.None, чтобы сообщение об успехе точно отправилось
        }
        catch (OperationCanceledException)
        {
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Operation canceled by user.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the media processing flow for session {MessageId}", session.StatusMessageId);
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, $"An unexpected error occurred:\n`{ex.Message}`", parseMode: ParseMode.Markdown);
        }
    }
}