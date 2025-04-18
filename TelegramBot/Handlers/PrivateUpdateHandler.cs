// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using Telegram.Bot.Types;
using DataBase.Types;
using TelegramMediaRelayBot.TelegramBot.Menu;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;


namespace TelegramMediaRelayBot.TelegramBot.Handlers;

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
            TGBot.userStates[chatId] = new ProcessVideoDC(link, statusMessage, text, timeoutCTS);

            if (defaultActionData == UsersAction.NO_VALUE) return;

            string defaultAction = defaultActionData.Split(';')[0];
            int defaultCondition = int.Parse(defaultActionData.Split(';')[1]);

            if (defaultAction == UsersAction.OFF) return;

            PrivateUtils.ProcessDefaultSendAction(botClient, chatId, statusMessage, defaultAction, cancellationToken,
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

    public static async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        var callbackQuery = update.CallbackQuery;
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);

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

            case "show_settings":
                await Users.ViewSettings(botClient, update);
                break;
            case "default_actions_menu":
                await Users.ViewDefaultActionsMenu(botClient, update);
                break;

            case "privacy_menu_and_safety":
                await Users.ViewPrivacyMenu(botClient, update);
                break;
            case "user_update_self_link":
                await Users.ViewLinkPrivacyMenu(botClient, update);
                break;
            case "user_update_self_link_with_contacts":
                await CommonUtilities.SendMessage(botClient, update, 
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup(
                        $"process_user_update_self_link_with_contacts", 
                        $"user_update_self_link"),
                    cancellationToken, 
                    Config.GetResourceString("UpdateLinkKeepContactsConfirmation"));
                break;

            case "user_update_self_link_with_new_contacts":
                await CommonUtilities.SendMessage(botClient, update, 
                    KeyboardUtils.GetConfirmForActionKeyboardMarkup(
                        $"process_user_update_self_link_with_new_contacts", 
                        $"user_update_self_link"),
                    cancellationToken, 
                    Config.GetResourceString("UpdateLinkNewContactsWarning"));
                break;

            case "user_update_self_link_with_keep_selected_contacts":
                await CommonUtilities.SendMessage(
                    botClient,
                    update,
                    KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
                    cancellationToken,
                    Config.GetResourceString("EnterContactIdsPrompt"));
                TelegramBot.userStates[chatId] = new ProcessContactLinksState(false);
                break;

            case "user_update_self_link_with_delete_selected_contacts":
                await CommonUtilities.SendMessage(
                    botClient,
                    update,
                    KeyboardUtils.GetReturnButtonMarkup("user_update_self_link"),
                    cancellationToken,
                    Config.GetResourceString("EnterContactIdsPrompt"));
                TelegramBot.userStates[chatId] = new ProcessContactLinksState(true);
                break;
            case "process_user_update_self_link_with_contacts":
                CoreDB.ReCreateSelfLink(DBforGetters.GetUserIDbyTelegramID(chatId));
                await Users.ViewLinkPrivacyMenu(botClient, update);
                break;
            case "process_user_update_self_link_with_new_contacts":
                CoreDB.ReCreateSelfLink(userId);
                ContactRemover.RemoveAllContacts(userId);
                await Users.ViewLinkPrivacyMenu(botClient, update);
                break;

            case "user_set_auto_send_video_time":
                await Users.ViewAutoSendVideoTimeMenu(botClient, update);
                break;
            case "video_default_actions_menu":
                await Users.ViewVideoDefaultActionsMenu(botClient, update);
                break;
            case "user_set_video_send_users":
                await Users.ViewUsersVideoSentUsersActionsMenu(botClient, update);
                break;

            case "whos_the_genius":
                await CallbackQueryMenuUtils.WhosTheGenius(botClient, update);
                break;

            default:
                if (callbackQuery.Data!.StartsWith("user_show_outbound_invite:"))
                {
                    await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId);
                }
                else if (callbackQuery.Data!.StartsWith("user_set_auto_send_video_time_to:"))
                {
                    string time = callbackQuery.Data.Split(':')[1];
                    bool result = Users.SetAutoSendVideoTimeToUser(chatId, time);

                    if (result)
                    {
                        await CommonUtilities.SendMessage(
                            botClient,
                            update,
                            KeyboardUtils.GetReturnButtonMarkup("user_set_auto_send_video_time"),
                            cancellationToken,
                            Config.GetResourceString("AutoSendTimeChangedMessage") + time
                        );
                        return;
                    }
                    await CommonUtilities.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetReturnButtonMarkup("user_set_auto_send_video_time"),
                        cancellationToken,
                        Config.GetResourceString("AutoSendTimeNotChangedMessage")
                    );
                }
                else if (callbackQuery.Data!.StartsWith("user_set_video_send_users:"))
                {
                    string action = callbackQuery.Data.Split(':')[1];

                    List<string> extendActions = new List<string>
                                                    {
                                                        UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS,
                                                        UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS,
                                                    };

                    if (extendActions.Contains(action))
                    {
                        await CommonUtilities.SendMessage(
                            botClient,
                            update,
                            KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"),
                            cancellationToken,
                            Config.GetResourceString("DefaultActionGetGroupOrUserIDs")
                        );

                        bool isGroup = false;
                        if (action == UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS) isGroup = true;

                        Users.SetDefaultActionToUser(chatId, action);
                        TGBot.userStates[chatId] = new ProcessUserSetDCSendState(isGroup);
                        return;
                    }


                    bool result = Users.SetDefaultActionToUser(chatId, action);

                    if (result)
                    {
                        await CommonUtilities.SendMessage(
                            botClient,
                            update,
                            KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"),
                            cancellationToken,
                            Config.GetResourceString("DefaultActionChangedMessage")
                        );
                        return;
                    }
                    await CommonUtilities.SendMessage(
                        botClient,
                        update,
                        KeyboardUtils.GetReturnButtonMarkup("user_set_video_send_users"),
                        cancellationToken,
                        Config.GetResourceString("DefaultActionNotChangedMessage")
                    );
                }
                break;
        }
    }
}