// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using System.Text.RegularExpressions;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Utils;
using Microsoft.Extensions.Hosting;
using TelegramMediaRelayBot.Config;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.TelegramBot.States;
using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using Telegram.Bot.Polling; // <-- Добавь этот using

namespace TelegramMediaRelayBot;

public partial class TGBot : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    
    // Наши "мозги"
    private readonly IUserStateManager _stateManager;
    private readonly StateHandlerFactory _stateHandlerFactory;
    private readonly DownloadSessionManager _sessionManager;
    private readonly ITelegramInteractionService _interactionService;

    // 1. ДОБАВЛЯЕМ НОВЫЕ СЕРВИСЫ для замены static полей
    private readonly ILastUserTextCache _lastUserTextCache;
    
    public TGBot(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        IUserStateManager userStateManager,
        StateHandlerFactory stateHandlerFactory,
        DownloadSessionManager sessionManager,
        ILastUserTextCache lastUserTextCache,
        ITelegramInteractionService interactionService)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _stateManager = userStateManager;
        _stateHandlerFactory = stateHandlerFactory;
        _sessionManager = sessionManager;
        _lastUserTextCache = lastUserTextCache;
        _interactionService = interactionService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("TGBot Hosted Service is starting.");

        var me = _botClient.GetMe(cancellationToken).GetAwaiter().GetResult();
        Log.Information("Hello, I am {BotId} ready and my name is {BotName}.", me.Id, me.FirstName);

        _botClient.StartReceiving(
            updateHandler: UpdateHandler,
            errorHandler: _errorHandler, // Убедись, что этот метод доступен
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cancellationToken
        );
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("TGBot Hosted Service is stopping.");
        return Task.CompletedTask;
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = _interactionService.GetChatId(update);
        if (_checkNonZeroID(chatId)) return;

        LogEvent(update, chatId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        if (_stateManager.TryGet(chatId, out var stateData))
        {
            var handler = _stateHandlerFactory.GetHandler(stateData.StateName);
            if (handler != null)
            {
                var result = await handler.Process(stateData, update, botClient, cancellationToken);
                
                if (result.NextAction == StateResultAction.Complete) _stateManager.Remove(chatId);
                else if (result.NextAction == StateResultAction.Continue) _stateManager.Set(chatId, stateData);
                
                return;
            }
        }

        if (update.Type == UpdateType.CallbackQuery)
        {
            var handlersFactory = sp.GetRequiredService<CallbackQueryHandlersFactory>();
            await handlersFactory.ExecuteAsync(update.CallbackQuery.Data, update, botClient, cancellationToken);
        }
        else if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            if (update.Message.Text.StartsWith("/")) { /* TODO */ }
            else if (_tryExtractLinkAndText(update.Message.Text, out var url, out var caption))
            {
                // ИСПРАВЛЕНИЕ №1: Получаем IResourceService из локального scope
                var resourceService = sp.GetRequiredService<Config.Services.IResourceService>();
                await CreateNewDownloadSession(botClient, update, url, caption, resourceService, cancellationToken);
            }
            else
            {
                // ИСПРАВЛЕНИЕ №3: Используем новый сервис вместо static
                _lastUserTextCache.Set(chatId, update.Message.Text);
            }
        }
    }

    private async Task CreateNewDownloadSession(ITelegramBotClient botClient, Update update, string url, string caption, Config.Services.IResourceService resourceService, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        
        var statusMessage = await botClient.SendMessage(
            chatId: chatId,
            text: resourceService.GetResourceString("VideoDistributionQuestion"),
            replyParameters: new ReplyParameters { MessageId = message.MessageId },
            replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(0),
            cancellationToken: cancellationToken
        );
        
        await botClient.EditMessageReplyMarkup(
            chatId: chatId,
            messageId: statusMessage.MessageId,
            replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(statusMessage.MessageId),
            cancellationToken: cancellationToken
        );
        
        _sessionManager.CreateSession(
            statusMessageId: statusMessage.MessageId,
            chatId: chatId,
            url: url,
            caption: caption,
            originalMessageDateUtc: message.Date.ToUniversalTime()
        );
        // TODO: Логика таймера
    }

    public void LogEvent(Update update, long chatId)
    {
        string currentUserStateName = "";
        string logMessageType;
        string logMessageData;
        long userId;

        if (update.CallbackQuery != null)
        {
            logMessageType = "CallbackQuery";
            logMessageData = update.CallbackQuery.Data!;
            userId = update.CallbackQuery.From.Id;
        }
        else if (update.Message != null)
        {
            if (update.Message.Text != null)
            {
                logMessageType = "Message";
                logMessageData = update.Message.Text;
                userId = update.Message.From!.Id;

                if (!_checkPrivateChatType(update))
                {
                    if (!update.Message.Text.Contains("/link") && !update.Message.Text.Contains("/help")) return;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        // ИСПРАВЛЕНИЕ №2: Адаптируем под UserStateData
        if (_stateManager.TryGet(chatId, out var stateData))
        {
            currentUserStateName = stateData?.StateName ?? "Unknown";
        }

        Log.Information("Event: {Type}, UserId: {UserId}, ChatId: {ChatId}, Data: {Data}, State: {State}",
            logMessageType, userId, chatId, logMessageData, currentUserStateName);
    }


    private bool _checkPrivateChatType(Update update)
    {
        if (update.Message != null && update.Message.Chat.Type == ChatType.Private) return true;
        if (update.CallbackQuery != null && update.CallbackQuery.Message!.Chat.Type == ChatType.Private) return true;
        return false;
    }

    private bool _tryExtractLinkAndText(string message, out string link, out string text)
    {
        link = string.Empty;
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(message)) return false;

        // 1) Ищем первый http(s) URL в любом месте строки
        var m = Regex.Match(message, @"https?://[^\s]+", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            link = m.Value.TrimEnd('.', ',', ';', '!', '?', ')', ']');
            // Подписью считаем ХВОСТ после первой ссылки; всё, что ДО ссылки — игнорируем
            int startAfterUrl = m.Index + m.Length;
            text = startAfterUrl < message.Length ? message[startAfterUrl..].Trim() : string.Empty;
            return true;
        }
        return false;
    }

    private Task _errorHandler(ITelegramBotClient _, Exception exception, CancellationToken __)
    {
        Log.Error($"Error occurred: {exception.Message}");
        Log.Error($"Stack trace: {exception.StackTrace}");

        if (exception.InnerException != null)
        {
            Log.Error($"Inner exception: {exception.InnerException.Message}");
            Log.Error($"Inner exception stack trace: {exception.InnerException.StackTrace}");
        }

        return Task.CompletedTask;
    }

    private bool _checkNonZeroID(long id)
    {
        if (id == 0) return true;
        return false;
    }
}