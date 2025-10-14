// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Handlers;

public class GroupUpdateHandler
{
    private readonly DownloadSessionManager _sessionManager;
    private readonly IUrlParsingService _urlParsingService;
    private readonly Config.Services.IResourceService _resourceService;

    // 1. Конструктор теперь чистый и не зависит от TGBot
    public GroupUpdateHandler(
        DownloadSessionManager sessionManager,
        IUrlParsingService urlParsingService,
        Config.Services.IResourceService resourceService)
    {
        _sessionManager = sessionManager;
        _urlParsingService = urlParsingService;
        _resourceService = resourceService;
    }

    public async Task HandleGroupUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var messageText = message.Text!;

        if (messageText.StartsWith("/link"))
        {
            await HandleLinkCommand(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/help"))
        {
            var text = _resourceService.GetResourceString("GroupHelpText");
            await botClient.SendMessage(message.Chat.Id, text, cancellationToken: cancellationToken);
        }
    }

    private async Task HandleLinkCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        // 2. Логика парсинга команды. Можно в будущем вынести в отдельный CommandParser.
        var parts = message.Text!.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await botClient.SendMessage(message.Chat.Id, _resourceService.GetResourceString("InvalidLinkFormat"), cancellationToken: cancellationToken);
            return;
        }
        
        var url = parts[1];
        var caption = message.ReplyToMessage?.Text ?? string.Empty; // Пример получения подписи из реплая
        
        // 3. Используем наш новый сервис для валидации ссылки
        if (!_urlParsingService.IsLink(url))
        {
            await botClient.SendMessage(message.Chat.Id, _resourceService.GetResourceString("InvalidLinkFormat"), cancellationToken: cancellationToken);
            return;
        }

        // 4. Вместо вызова HandleMediaRequest, создаем сессию через менеджер.
        // Для групповых чатов мы не показываем клавиатуру, а сразу запускаем загрузку "только для себя".
        // (Это предположение, логику можно изменить).
        
        var statusMessage = await botClient.SendMessage(message.Chat.Id, 
            _resourceService.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);

        var session = _sessionManager.CreateSession(
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