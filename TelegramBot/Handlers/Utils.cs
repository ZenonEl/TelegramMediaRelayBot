// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.TelegramBot.Handlers;

class PrivateUtils
{
    private readonly TGBot _tgBot;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;

    public PrivateUtils(
        TGBot tgBot,
        IContactGetter contactGetterRepository,
        IDefaultActionGetter defaultActionGetter,
        IUserGetter userGetter,
        IGroupGetter groupGetter
        )
    {
        _tgBot = tgBot;
        _contactGetterRepository = contactGetterRepository;
        _defaultActionGetter = defaultActionGetter;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
    }

    public void ProcessDefaultSendAction(ITelegramBotClient botClient, long chatId, Message statusMessage, string defaultAction,
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

                int actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
                mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);

                switch (defaultAction)
                {
                    case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                        List<long> contactUserTGIds = await _contactGetterRepository.GetAllContactUserTGIds(userId);
                        targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();
                        break;

                    case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                        userIds = await _groupGetter.GetAllUsersInDefaultEnabledGroups(userId);

                        targetUserIds = userIds
                            .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                            .Select(_userGetter.GetTelegramIDbyUserID)
                            .ToList();
                        break;

                    case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                        List<int> groupIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);

                        foreach (int groupId in groupIds)
                        {
                            userIds.AddRange(await _groupGetter.GetAllUsersIdsInGroup(groupId));
                        }

                        targetUserIds = userIds
                            .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                            .Select(_userGetter.GetTelegramIDbyUserID)
                            .ToList();
                        break;

                    case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                        userIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);

                        targetUserIds = userIds
                            .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                            .Select(_userGetter.GetTelegramIDbyUserID)
                            .ToList();
                        break;
                }

                if (TGBot.userStates.TryGetValue(chatId, out var state) && state is ProcessVideoDC videoState)
                {
                    await botClient.EditMessageText(
                        statusMessage.Chat.Id,
                        statusMessage.MessageId,
                        Config.GetResourceString("DefaultActionTimeoutMessage"),
                        cancellationToken: cancellationToken
                    );
                    _ = _tgBot.HandleMediaRequest(botClient, link, chatId, statusMessage, targetUserIds, caption: text);

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
                        TGBot.userStates.Remove(chatId, out _);
                    }
                }
            }
            catch (TaskCanceledException) { }
        }, cancellationToken);
    }
}