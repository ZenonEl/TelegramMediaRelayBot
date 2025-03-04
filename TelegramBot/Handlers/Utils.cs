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
using TelegramMediaRelayBot;
using DataBase;
using DataBase.Types;


namespace MediaTelegramBot.HandlersUtils;

class PrivateUtils
{

    public static void ProcessDefaultSendAction(ITelegramBotClient botClient, long chatId, Message statusMessage, string defaultAction,
                                                CancellationToken cancellationToken, int userId, int defaultCondition, CancellationTokenSource timeoutCTS,
                                                string link, string text)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(defaultCondition), timeoutCTS.Token);

                List<long> targetUserIds = new List<long>();
                List<long> mutedByUserIds = new List<long>();
                List<int> userIds = new List<int>();

                int actionId = DBforDefaultActions.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
                mutedByUserIds = DBforGetters.GetUsersIdForMuteContactId(userId);

                switch (defaultAction)
                {
                    case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                        List<long> contactUserTGIds = await CoreDB.GetAllContactUserTGIds(userId);
                        targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                        break;

                    case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                        userIds = DBforGroups.GetAllUsersInDefaultEnabledGroups(userId);

                        targetUserIds = userIds
                            .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                            .Select(DBforGetters.GetTelegramIDbyUserID)
                            .ToList();
                        break;

                    case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                        List<int> groupIds = DBforDefaultActions.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);

                        foreach (int groupId in groupIds)
                        {
                            userIds.AddRange(DBforGroups.GetAllUsersIdsInGroup(groupId));
                        }

                        targetUserIds = userIds
                            .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                            .Select(DBforGetters.GetTelegramIDbyUserID)
                            .ToList();
                        break;

                    case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                        userIds = DBforDefaultActions.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);

                        targetUserIds = userIds
                            .Where(contactId => !mutedByUserIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                            .Select(DBforGetters.GetTelegramIDbyUserID)
                            .ToList();
                        break;
                }

                if (TelegramBot.userStates.TryGetValue(chatId, out var state) && state is ProcessVideoDC videoState)
                {
                    await botClient.EditMessageText(
                        statusMessage.Chat.Id,
                        statusMessage.MessageId,
                        Config.GetResourceString("DefaultActionTimeoutMessage"),
                        cancellationToken: cancellationToken
                    );
                    _ = TelegramBot.HandleMediaRequest(botClient, link, chatId, statusMessage, targetUserIds, caption: text);

                    if (videoState.linkQueue.Count > 0)
                    {
                        var nextLink = videoState.linkQueue.Dequeue();
                        statusMessage = await botClient.EditMessageText(
                            chatId,
                            nextLink.MessageId,
                            Config.GetResourceString("WaitDownloadingVideo"),
                            cancellationToken: cancellationToken
                        );
                        ProcessDefaultSendAction(
                            botClient,
                            chatId,
                            statusMessage,
                            defaultAction,
                            cancellationToken,
                            userId,
                            defaultCondition,
                            timeoutCTS,
                            nextLink.Link,
                            nextLink.Text
                        );
                    }
                    else
                    {
                        TelegramBot.userStates.Remove(chatId, out _);
                    }
                }
            }
            catch (TaskCanceledException) { }
        }, cancellationToken);
    }
}