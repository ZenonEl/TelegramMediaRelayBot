using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Database.Interfaces;
// ... другие using'и ...

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ITelegramSenderService
{
    Task SendMedia(ITelegramBotClient botClient, DownloadSession session, List<byte[]> mediaFiles, List<long>? targetUserIds);
}

public class TelegramSenderService : ITelegramSenderService
{
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;
    // ... другие зависимости, которые были в SendMediaToTelegram/SendVideoToContacts ...

    public TelegramSenderService(IUserGetter userGetter, IPrivacySettingsGetter privacySettingsGetter)
    {
        _userGetter = userGetter;
        _privacySettingsGetter = privacySettingsGetter;
    }

    public async Task SendMedia(ITelegramBotClient botClient, DownloadSession session, List<byte[]> mediaFiles, List<long>? targetUserIds)
    {
        // ВЕСЬ КОД из твоих старых методов SendMediaToTelegram, SendVideoToContacts,
        // SendIndividually, GroupMediaFiles, ValidateMediaFilesOrReport
        // ПЕРЕЕЗЖАЕТ СЮДА.
        
        // Он будет использовать зависимости, полученные через конструктор,
        // и параметры, пришедшие в этот метод.
        
        Log.Information("Starting to send {FileCount} files for session {MessageId}", mediaFiles.Count, session.StatusMessageId);
        // ... твоя логика отправки ...
    }
}