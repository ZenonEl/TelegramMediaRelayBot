using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Infrastructure.MediaProcessing;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IMediaProcessingFlow
{
    Task StartFlow(ITelegramBotClient botClient, Update update, DownloadSession session, List<long>? targetUserIds = null);
}

public class MediaProcessingFlow : IMediaProcessingFlow
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly MediaDownloaderService _downloaderService;
    private readonly ITelegramSenderService _senderService;
    private readonly IUserGetter _userGetter;
    private readonly ICaptionGenerationService _captionGenerator;

    public MediaProcessingFlow(
        DownloadSessionManager sessionManager,
        MediaDownloaderService downloaderService,
        ITelegramSenderService senderService,
        IOptionsMonitor<DownloadingConfiguration> downloadingConfig,
        IUserGetter userGetter,
        ICaptionGenerationService captionGenerator)
    {
        _sessionManager = sessionManager;
        _downloaderService = downloaderService;
        _senderService = senderService;
        _userGetter = userGetter;
        _captionGenerator = captionGenerator;
    }

    public async Task StartFlow(ITelegramBotClient botClient, Update update, DownloadSession session, List<long>? targetUserIds = null)
    {
        try
        {
            // --- ЭТАП 1: СКАЧИВАНИЕ ---
            await botClient.EditMessageText(
                chatId: session.ChatId,
                messageId: session.StatusMessageId,
                text: "Downloading...",
                replyMarkup: KeyboardUtils.GetCancelKeyboardMarkup(session.StatusMessageId),
                cancellationToken: session.SessionCts.Token
            );
            
            await using TelegramLogUpdater logUpdater = new TelegramLogUpdater(botClient, session.StatusMessageId, session.ChatId);

            DownloadOptions options = new DownloadOptions { OnProgress = logUpdater.HandleLogLine };
            DownloadResult downloadResult = await _downloaderService.DownloadMedia(session.Url, options, session.SessionCts.Token);

            if (!downloadResult.Success)
            {
                string errorMessage = $"Download failed:\n`{downloadResult.ErrorMessage}`";
                await botClient.EditMessageText(session.ChatId, session.StatusMessageId, errorMessage, parseMode: ParseMode.Markdown);
                return;
            }

            // --- ЭТАП 2: ПОСТОБРАБОТКА ---
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Processing media...",
                                            replyMarkup: KeyboardUtils.GetCancelKeyboardMarkup(session.StatusMessageId),
                                            cancellationToken: session.SessionCts.Token);

            List<byte[]> processedFiles = downloadResult.MediaFiles;

            // --- ЭТАП 3: ОТПРАВКА ---
            await botClient.EditMessageText(session.ChatId, session.StatusMessageId, "Sending media...",
                                            replyMarkup: KeyboardUtils.GetCancelKeyboardMarkup(session.StatusMessageId),
                                            cancellationToken: session.SessionCts.Token);
            var senderName = _userGetter.GetUserNameByTelegramID(session.ChatId);
            session.Caption = _captionGenerator.Generate(session, senderName);

            await _senderService.SendMedia(botClient, update, session, processedFiles, targetUserIds, session.SessionCts.Token);
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
        finally
        {
            _sessionManager.CompleteSession(session.StatusMessageId);
        }
    }
}