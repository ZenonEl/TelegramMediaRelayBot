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
    private TGBot _tgBot;

    public GroupUpdateHandler(
        TGBot tgBot)
    {
        _tgBot = tgBot;
    }

    public async Task HandleGroupUpdate(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string messageText = update.Message!.Text!;
        if (messageText.Contains("/link"))
        {
            string link;
            string text = "";

            string trimmedMessage = messageText[5..].Trim();

            int separatorIndex = trimmedMessage.IndexOfAny([' ', '\n']);

            if (separatorIndex != -1)
            {
                link = trimmedMessage[..separatorIndex].Trim();
                text = trimmedMessage[separatorIndex..].Trim();
            }
            else
            {
                link = trimmedMessage.Trim();
            }

            if (CommonUtilities.IsLink(link))
            {
                Message statusMessage = await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("WaitDownloadingVideo"), cancellationToken: cancellationToken);
                _ = _tgBot.HandleMediaRequest(botClient, link, update.Message.Chat.Id, statusMessage: statusMessage, groupChat: true, caption: text);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("InvalidLinkFormat"), cancellationToken: cancellationToken);
            }
        }
        else if (messageText == "/help")
        {
            string text = Config.GetResourceString("GroupHelpText");
            await botClient.SendMessage(update.Message.Chat.Id, text, cancellationToken: cancellationToken);
        }
    }
}