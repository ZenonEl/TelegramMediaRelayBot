// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers;

public class GroupUpdateHandler
{
    private readonly IStatusResourceService _statusResources;
    private readonly IHelpResourceService _helpResources;
    private readonly IErrorsResourceService _errorsResources;
    private readonly DownloadSessionManager _sessionManager;
    private readonly IUrlParsingService _urlParsingService;
    private readonly Config.Services.IResourceService _resourceService;

    // 1. Конструктор теперь чистый и не зависит от TGBot
    public GroupUpdateHandler(
        IStatusResourceService statusResources,
        IHelpResourceService helpResources,
        IErrorsResourceService errorsResources,
        DownloadSessionManager sessionManager,
        IUrlParsingService urlParsingService,
        Config.Services.IResourceService resourceService)
    {
        _statusResources = statusResources;
        _helpResources = helpResources;
        _errorsResources = errorsResources;
        _sessionManager = sessionManager;
        _urlParsingService = urlParsingService;
        _resourceService = resourceService;
    }

    public async Task HandleGroupUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Message message = update.Message!;
        string messageText = message.Text!;

        if (messageText.StartsWith("/link"))
        {
            await HandleLinkCommand(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/help"))
        {
            string text = _helpResources.GetString("Help.Group");
            await botClient.SendMessage(message.Chat.Id, text, cancellationToken: cancellationToken);
        }
    }

    private async Task HandleLinkCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        // 2. Логика парсинга команды. Можно в будущем вынести в отдельный CommandParser.
        string[] parts = message.Text!.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await botClient.SendMessage(message.Chat.Id, _errorsResources.GetString("Error.InvalidLinkFormat"), cancellationToken: cancellationToken);
            return;
        }

        string url = parts[1];
        string caption = message.ReplyToMessage?.Text ?? string.Empty; // Пример получения подписи из реплая

        // 3. Используем наш новый сервис для валидации ссылки
        if (!_urlParsingService.IsLink(url))
        {
            await botClient.SendMessage(message.Chat.Id, _errorsResources.GetString("Error.InvalidLinkFormat"), cancellationToken: cancellationToken);
            return;
        }

        // 4. Вместо вызова HandleMediaRequest, создаем сессию через менеджер.
        // Для групповых чатов мы не показываем клавиатуру, а сразу запускаем загрузку "только для себя".
        // (Это предположение, логику можно изменить).

        Message statusMessage = await botClient.SendMessage(message.Chat.Id,
            _statusResources.GetString("Status.ProcessingLink"), cancellationToken: cancellationToken);

        DownloadSession session = _sessionManager.CreateSession(
            statusMessageId: statusMessage.MessageId,
            chatId: message.Chat.Id,
            url: url,
            caption: caption,
            originalMessageDateUtc: message.Date.ToUniversalTime()
        );

        // TODO: Здесь мы должны запустить саму загрузку, вызвав MediaDownloaderService.
        // В TGBot мы это делали через _tgBot.HandleMediaRequest, а здесь нужно будет
        // получить MediaDownloaderService из DI и вызвать его.
        // Например: _downloaderService.DownloadMedia(url, options, session.SessionCts.Token);
    }
}
