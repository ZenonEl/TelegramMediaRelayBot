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
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.TelegramBot.Handlers;

public class PrivateUpdateHandler
{
    private readonly TGBot _tgBot;
    private readonly CallbackQueryHandlersFactory _handlersFactory;
    private readonly IContactGetter _contactGetterRepository;

    public PrivateUpdateHandler(
        TGBot tgBot,
        CallbackQueryHandlersFactory handlersFactory,
        IContactGetter contactGetterRepository)
    {
        _tgBot = tgBot;
        _handlersFactory = handlersFactory;
        _contactGetterRepository = contactGetterRepository;
    }

    public async Task ProcessMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        string messageText = update.Message!.Text!;
        string link;
        string text = "";

        int newLineIndex = messageText.IndexOf('\n');

        if (newLineIndex != -1)
        {
            link = messageText[..newLineIndex].Trim();
            text = messageText[(newLineIndex + 1)..].Trim();
        }
        else
        {
            link = messageText.Trim();
        }

        if (CommonUtilities.IsLink(link))
        {
            int replyToMessageId = update.Message.MessageId;
            Message statusMessage = await botClient.SendMessage(
                chatId,
                Config.GetResourceString("WaitDownloadingVideo"),
                replyParameters: new ReplyParameters { MessageId = replyToMessageId },
                cancellationToken: cancellationToken
            );

            await botClient.EditMessageText(
                statusMessage.Chat.Id,
                statusMessage.MessageId,
                Config.GetResourceString("VideoDistributionQuestion"),
                replyMarkup: KeyboardUtils.GetVideoDistributionKeyboardMarkup(),
                cancellationToken: cancellationToken
            );

            int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
            string defaultActionData = DBforGetters.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);

            CancellationTokenSource timeoutCTS = new CancellationTokenSource();
            TGBot.userStates[chatId] = new ProcessVideoDC(link, statusMessage, text, timeoutCTS, _tgBot, _contactGetterRepository);

            if (defaultActionData == UsersAction.NO_VALUE) return;

            string defaultAction = defaultActionData.Split(';')[0];
            int defaultCondition = int.Parse(defaultActionData.Split(';')[1]);

            if (defaultAction == UsersAction.OFF) return;
            var privateUtils = new PrivateUtils(_tgBot, _contactGetterRepository);
            privateUtils.ProcessDefaultSendAction(botClient, chatId, statusMessage, defaultAction, cancellationToken,
                                                                userId, defaultCondition, timeoutCTS, link, text);
        }
        else if (update.Message.Text == "/start")
        {
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        }
        else if (update.Message.Text == "/help")
        {
            string helpText = Config.GetResourceString("HelpText");
            await CommonUtilities.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, helpText);
        }
        else
        {
            await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("WhatShouldIDoWithThis"), cancellationToken: cancellationToken);
        }
    }

    public async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var callbackQuery = update.CallbackQuery;

        string data = callbackQuery!.Data!;
        int colonIndex = data.IndexOf(':');
        string commandName = colonIndex >= 0 ? data[..(colonIndex + 1)] : data;

        var command = _handlersFactory.GetCommand(commandName);
        
        await command.ExecuteAsync(update, botClient, cancellationToken);
    }
}
