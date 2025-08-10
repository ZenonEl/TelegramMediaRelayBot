// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Config.Services;


namespace TelegramMediaRelayBot.TelegramBot.Handlers;

public class PrivateUpdateHandler
{
    private readonly TGBot _tgBot;
    private readonly CallbackQueryHandlersFactory _handlersFactory;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IConfigurationService _configService;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;
    private readonly TelegramMediaRelayBot.TelegramBot.Utils.ITextCleanupService _textCleanup;

    public PrivateUpdateHandler(
        TGBot tgBot,
        CallbackQueryHandlersFactory handlersFactory,
        IContactGetter contactGetterRepository,
        IDefaultActionGetter defaultActionGetter,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        TelegramMediaRelayBot.Config.Services.IConfigurationService configService,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService,
        TelegramMediaRelayBot.TelegramBot.Utils.ITextCleanupService textCleanup
        )
    {
        _tgBot = tgBot;
        _handlersFactory = handlersFactory;
        _contactGetterRepository = contactGetterRepository;
        _defaultActionGetter = defaultActionGetter;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _configService = configService;
        _resourceService = resourceService;
        _textCleanup = textCleanup;
    }

    public async Task ProcessMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        string messageText = update.Message!.Text!;
        string link;
        string text = "";

        if (CommonUtilities.TryExtractLinkAndText(messageText, out var extractedLink, out var extractedText))
        {
            link = extractedLink;
            text = extractedText;
            // Очередь задержек: используем базовую задержку default-действия как окно
            var delayCfg = _tgBot.GetType(); // placeholder
            int replyToMessageId = update.Message.MessageId;
            
            Message statusMessage = await botClient.SendMessage(
                chatId,
                _resourceService.GetResourceString("WaitDownloadingVideo"),
                replyParameters: new ReplyParameters { MessageId = replyToMessageId },
                cancellationToken: cancellationToken
            );

            await botClient.EditMessageText(
                statusMessage.Chat.Id,
                statusMessage.MessageId,
                _resourceService.GetResourceString("VideoDistributionQuestion"),
                replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(),
                cancellationToken: cancellationToken
            );

            // Больше не чистим стартовый хвост доменными правилами: берём как есть (по договорённости)

            int userId = _userGetter.GetUserIDbyTelegramID(chatId);
            TGBot.StateManager.Set(chatId, new ProcessVideoDC(link, statusMessage, text, _tgBot, _contactGetterRepository, _userGetter, _groupGetter, _defaultActionGetter, _resourceService, _textCleanup));
            // Scheduling of default action moved inside state per-message
            // It will create its own CTS and handle cancellation on user interaction
            await Task.Yield();
        }
        else if (update.Message.Text == "/start")
        {
            // мгновенная отмена всех сессий чата
            if (TGBot.StateManager.TryGet(chatId, out var state) && state is ProcessVideoDC)
            {
                // просто сбрасываем state; конкретные CTS будут отменены при первой кнопке
                TGBot.StateManager.Remove(chatId);
            }
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        }
        else if (update.Message.Text == "/help")
        {
            string helpText = _resourceService.GetResourceString("HelpText");
            await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, helpText);
        }
        else
        {
            // Сохраняем "предыдущий текст" для окна caption, если это не ссылка
            TGBot.RememberLastText(chatId, update.Message.Text);
            await botClient.SendMessage(update.Message.Chat.Id, _resourceService.GetResourceString("WhatShouldIDoWithThis"), cancellationToken: cancellationToken);
        }
    }

    public async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var callbackQuery = update.CallbackQuery;

        string data = callbackQuery!.Data!;
        int colonIndex = data.IndexOf(':');
        string commandName = colonIndex >= 0 ? data[..(colonIndex + 1)] : data;

        await _handlersFactory.ExecuteAsync(commandName, update, botClient, cancellationToken);
    }
}
