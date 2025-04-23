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
using TelegramMediaRelayBot.TelegramBot.Menu;
using TelegramMediaRelayBot.Database;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;



public class ShowOutboundInviteCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_show_outbound_invite";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;
        await CallbackQueryMenuUtils.ShowOutboundInvite(botClient, update, chatId);
    }
}

public class SetAutoSendTimeCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_auto_send_video_time_to:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string callbackQueryData = update.CallbackQuery!.Data!.Split(':')[1];
        var chatId = update.CallbackQuery!.Message!.Chat.Id;
        bool result = Users.SetAutoSendVideoTimeToUser(chatId, callbackQueryData);

        var message = result 
            ? Config.GetResourceString("AutoSendTimeChangedMessage") + callbackQueryData
            : Config.GetResourceString("AutoSendTimeNotChangedMessage");

        await CommonUtilities.SendMessage(
            botClient,
            update,
            KeyboardUtils.GetReturnButtonMarkup("user_set_auto_send_video_time"),
            ct,
            message
        );
    }
}

public class SetVideoSendUsersCommand : IBotCallbackQueryHandlers
{
    public string Name => "user_set_video_send_users:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string action = update.CallbackQuery!.Data!.Split(':')[1];
        long chatId = update.CallbackQuery!.Message!.Chat.Id;

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
}