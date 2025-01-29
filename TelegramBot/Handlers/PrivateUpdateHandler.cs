// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Telegram.Bot;
using Telegram.Bot.Types;
using MediaTelegramBot.Menu;
using MediaTelegramBot.Utils;
using TelegramMediaRelayBot;
using DataBase;
using DataBase.Types;


namespace MediaTelegramBot;

public class PrivateUpdateHandler
{

    public static async Task ProcessMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
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

        if (Utils.Utils.IsLink(link))
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
            TelegramBot.userStates[chatId] = new ProcessVideoDC(link, statusMessage, text, timeoutCTS);

            if (defaultActionData == UsersAction.NO_VALUE) return;

            string defaultAction = defaultActionData.Split(';')[0];
            int defaultCondition = int.Parse(defaultActionData.Split(';')[1]);

            List<string> excludedActions = new List<string>
                                            {
                                                UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS,
                                                UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS
                                            };

            if (excludedActions.Contains(defaultAction)) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(defaultCondition), timeoutCTS.Token);

                    List<long> targetUserIds = new List<long>();
                    List<long> mutedByUserIds = new List<long>();

                    if (defaultAction == UsersAction.SEND_MEDIA_TO_ALL_CONTACTS)
                    {
                        mutedByUserIds = DBforGetters.GetUsersIdForMuteContactId(userId);
                        List<long> contactUserTGIds = await CoreDB.GetAllContactUserTGIds(userId);
                        targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                    }
                    else if (defaultAction == UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS)
                    {
                        List<int> defaultGroupContactIDs = DBforGroups.GetAllUsersInDefaultEnabledGroups(userId);

                        targetUserIds = defaultGroupContactIDs
                            .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                            .Select(DBforGetters.GetTelegramIDbyUserID)
                            .ToList();
                    }

                    if (TelegramBot.userStates.TryGetValue(chatId, out var state) && state is ProcessVideoDC)
                    {
                        await botClient.EditMessageText(
                            statusMessage.Chat.Id,
                            statusMessage.MessageId,
                            "⏳ Время вышло, будет выполнено действие по умолчанию...",
                            cancellationToken: cancellationToken
                        );

                        _ = TelegramBot.HandleVideoRequest(botClient, link, chatId, statusMessage, targetUserIds, caption: text);
                        
                        TelegramBot.userStates.Remove(chatId, out _);
                    }
                }
                catch (TaskCanceledException) {}
            }, cancellationToken);
        }
        else if (update.Message.Text == "/start")
        {
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        }
        else if (update.Message.Text == "/help")
        {
            string helpText = Config.GetResourceString("HelpText");
            await Utils.Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, helpText);
        }
        else
        {
            await botClient.SendMessage(update.Message.Chat.Id, Config.GetResourceString("WhatShouldIDoWithThis"), cancellationToken: cancellationToken);
        }
    }

    public static async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        var callbackQuery = update.CallbackQuery;

        switch (callbackQuery!.Data)
        {
            case "main_menu":
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
            case "add_contact":
                await Contacts.AddContact(botClient, update, chatId);
                break;
            case "get_self_link":
                await CallbackQueryMenuUtils.GetSelfLink(botClient, update);
                break;
            case "view_inbound_invite_links":
                await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, chatId);
                break;
            case "view_outbound_invite_links":
                await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update);
                break;
            case "view_contacts":
                await Contacts.ViewContacts(botClient, update);
                break;
            case "show_groups":
                await Groups.ViewGroups(botClient, update, cancellationToken);
                break;
            case "mute_contact":
                await Contacts.MuteUserContact(botClient, update, chatId);
                break;
            case "unmute_contact":
                await Contacts.UnMuteUserContact(botClient, update, chatId);
                break;
            case "edit_contact_group":
                await Contacts.EditContactGroup(botClient, update, chatId);
                break;
            case "delete_contact":
                await Contacts.DeleteContact(botClient, update, chatId);
                break;
            case "whos_the_genius":
                await CallbackQueryMenuUtils.WhosTheGenius(botClient, update);
                break;
            default:
                if (callbackQuery.Data!.StartsWith("user_show_outbound_invite:"))
                {
                    await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId);
                }
                break;
        }
    }
}