// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;
using TelegramBot.Services;
using TelegramMediaRelayBot.Config;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers;

public class PrivateUpdateHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DownloadSessionManager _sessionManager;
    private readonly ILastUserTextCache _lastUserTextCache;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;
    private readonly IOptions<BotConfiguration> _botConfig;
    private readonly IConfigurationService _configService;
    private readonly IResourceService _resourceService;
    private readonly IUrlParsingService _urlParser;
    private readonly IStartParameterParser _startParser;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IMediaDownloaderFactory _downloaderFactory;
    private readonly IDefaultActionGetter _defaultActionGetter;

    public PrivateUpdateHandler(
        IServiceScopeFactory scopeFactory,
        DownloadSessionManager sessionManager,
        ILastUserTextCache lastUserTextCache,
        IUserRepository userRepository,
        IUserGetter userGetter,
        IOptions<BotConfiguration> botConfig,
        IConfigurationService configService,
        IResourceService resourceService,
        IUrlParsingService urlParser,
        IStartParameterParser startParser,
        ITelegramInteractionService interactionService,
        IMediaDownloaderFactory downloaderFactory,
        IDefaultActionGetter defaultActionGetter)
    {
        _scopeFactory = scopeFactory;
        _sessionManager = sessionManager;
        _lastUserTextCache = lastUserTextCache;
        _userRepository = userRepository;
        _userGetter = userGetter;
        _botConfig = botConfig;
        _configService = configService;
        _resourceService = resourceService;
        _urlParser = urlParser;
        _startParser = startParser;
        _interactionService = interactionService;
        _downloaderFactory = downloaderFactory;
        _defaultActionGetter = defaultActionGetter;
    }

    public async Task ProcessMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Message message = update.Message!;
        long chatId = message.Chat.Id;

        if (!await EnsureUserHasAccessOrRegister(botClient, update, cancellationToken)) return;

        if (message.Text != null && message.Text.StartsWith("/"))
        {
            await HandleCommand(botClient, update, cancellationToken);
            return;
        }

        if (message.Text != null && _urlParser.TryExtractLinkAndText(message.Text, out string? url, out string? caption))
        {
            IEnumerable<IMediaDownloader> downloaders = _downloaderFactory.GetDownloadersForUrl(url);
            if (!downloaders.Any())
            {
                //TODO Отправка текста что ссылка не поддерживается
                Log.Debug("No suitable downloader found for URL: {Url}. Ignoring.", url);
                return;
            }

            Message statusMessage = await botClient.SendMessage(chatId, _resourceService.GetResourceString("VideoDistributionQuestion"),
                replyParameters: new ReplyParameters { MessageId = message.MessageId },
                replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(0), cancellationToken: cancellationToken);

            await botClient.EditMessageReplyMarkup(chatId, statusMessage.MessageId,
                replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(statusMessage.MessageId),
                cancellationToken: cancellationToken);

            DownloadSession session = _sessionManager.CreateSession(
                statusMessageId: statusMessage.MessageId,
                chatId: chatId, url: url, caption: caption,
                originalMessageDateUtc: message.Date);

            _sessionManager.ScheduleDefaultAction(botClient, update, session);
        }
        else if (message.Text != null && _sessionManager.GetLatestPendingSession(chatId) != null)
        {
            DownloadSession? session = _sessionManager.GetLatestPendingSession(chatId);
            int userId = _userGetter.GetUserIDbyTelegramID(chatId);
            string defaultAction = await _defaultActionGetter.GetDefaultActionByUserIDAndTypeAsync(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            int actionCondition = 30;

            if (defaultAction != UsersAction.OFF && defaultAction != UsersAction.NO_VALUE)
                int.TryParse(defaultAction.Split(";")[1], out actionCondition);
            TimeSpan window = TimeSpan.FromSeconds(actionCondition);

            if (session != null && (DateTime.UtcNow - session.CreatedAtUtc) <= window)
            {
                _sessionManager.UpdateCaption(session.StatusMessageId, message.Text);
                await botClient.SendMessage(message.Chat.Id, "Caption updated.");
            }
            else
            {
                _lastUserTextCache.Set(chatId, message.Text);
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("WhatShouldIDoWithThis"), cancellationToken: cancellationToken);
            }
        }
        else if (message.Text != null)
        {
            _lastUserTextCache.Set(chatId, message.Text);
            await botClient.SendMessage(chatId, _resourceService.GetResourceString("WhatShouldIDoWithThis"), cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(chatId, _resourceService.GetResourceString("WhatShouldIDoWithThis"), cancellationToken: cancellationToken);
        }
    }

    public async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        CallbackQueryHandlersFactory handlersFactory = scope.ServiceProvider.GetRequiredService<CallbackQueryHandlersFactory>();
        CallbackQuery callbackQuery = update.CallbackQuery!;
        string data = callbackQuery.Data!;

        await handlersFactory.ExecuteAsync(update, botClient, cancellationToken);
    }

    private async Task<bool> EnsureUserHasAccessOrRegister(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = update.Message!.Chat.Id;
        if (_userRepository.CheckUserExists(chatId)) return true;

        int usersCount = await _userGetter.GetAllUsersCount();
        // Используем наш новый сервис
        string startParameter = _startParser.Parse(update.Message.Text!);

        if ((usersCount == 0 || !string.IsNullOrEmpty(startParameter)) && _configService.CanUserStartUsingBot(startParameter, _userGetter))
        {
            _userRepository.AddUser(update.Message.Chat.FirstName!, chatId, false);
            update.Message.Text = "/start";
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_botConfig.Value.AccessDeniedMessageContact))
        {
            await botClient.SendMessage(chatId,
                string.Format(_resourceService.GetResourceString("AccessDeniedMessage"), _botConfig.Value.AccessDeniedMessageContact),
                cancellationToken: cancellationToken, parseMode: ParseMode.Html);
        }
        return false;
    }

    private async Task HandleCommand(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IServiceProvider sp = scope.ServiceProvider;
        IInboxRepository inboxRepo = sp.GetRequiredService<IInboxRepository>();
        IUserMenuService menuService = sp.GetRequiredService<IUserMenuService>();

        if (update.Message?.Text == "/start")
        {
            int userId = _userGetter.GetUserIDbyTelegramID(update.Message.Chat.Id);
            int newCount = await inboxRepo.GetNewCountAsync(userId);
            await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(newCount), cancellationToken);
        }
        else if (update.Message?.Text == "/help")
        {
            await menuService.ViewHelpMenu(botClient, update);
        }
    }
}
