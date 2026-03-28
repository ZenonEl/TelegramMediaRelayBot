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

namespace TelegramMediaRelayBot.TelegramBot.Handlers;

public class GroupUpdateHandler
{
    private readonly TGBot _tgBot;

    public GroupUpdateHandler(
        TGBot tgBot)
    {
        _tgBot = tgBot;
    }

    public async Task HandleGroupUpdate(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string messageText = update.Message!.Text!;
        long chatId = update.Message.Chat.Id;

        if (messageText.StartsWith("/link"))
        {
            await HandleLinkCommand(messageText, chatId, botClient, cancellationToken);
        }
        else if (messageText == "/help")
        {
            string text = Config.GetResourceString("GroupHelpText");
            await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
        }
        else
        {
            await HandlePlainUrl(messageText, chatId, update.Message.MessageId, botClient, cancellationToken);
        }
    }

    private async Task HandleLinkCommand(string messageText, long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string link;
        string caption = "";

        string trimmedMessage = messageText[5..].Trim();

        int separatorIndex = trimmedMessage.IndexOfAny([' ', '\n']);

        if (separatorIndex != -1)
        {
            link = trimmedMessage[..separatorIndex].Trim();
            caption = trimmedMessage[separatorIndex..].Trim();
        }
        else
        {
            link = trimmedMessage.Trim();
        }

        if (CommonUtilities.IsLink(link))
        {
            await DownloadAndSend(botClient, link, chatId, caption, cancellationToken);
        }
        else
        {
            await botClient.SendMessage(chatId, Config.GetResourceString("InvalidLinkFormat"), cancellationToken: cancellationToken);
        }
    }

    private async Task HandlePlainUrl(string messageText, long chatId, int replyToMessageId, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string link;
        string caption = "";

        int newLineIndex = messageText.IndexOf('\n');

        if (newLineIndex != -1)
        {
            link = messageText[..newLineIndex].Trim();
            caption = messageText[(newLineIndex + 1)..].Trim();
        }
        else
        {
            link = messageText.Trim();
        }

        if (CommonUtilities.IsLink(link))
        {
            await DownloadAndSend(botClient, link, chatId, caption, cancellationToken);
        }
    }

    private async Task DownloadAndSend(ITelegramBotClient botClient, string link, long chatId, string caption, CancellationToken cancellationToken)
    {
        Message statusMessage = await botClient.SendMessage(
            chatId,
            Config.GetResourceString("WaitDownloadingVideo"),
            cancellationToken: cancellationToken
        );

        try
        {
            await _tgBot.HandleMediaRequest(botClient, link, chatId, statusMessage: statusMessage, groupChat: true, caption: caption);
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Group media download was cancelled for chat {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process media in group chat {ChatId}", chatId);
            try
            {
                await botClient.EditMessageText(
                    statusMessage.Chat.Id,
                    statusMessage.MessageId,
                    Config.GetResourceString("FailedToProcessLink"),
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception editEx)
            {
                Log.Debug(editEx, "Failed to edit error status message");
            }
        }
    }
}
