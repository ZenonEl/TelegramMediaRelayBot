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
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public PrivateUtils(
        TGBot tgBot,
        IContactGetter contactGetterRepository,
        IDefaultActionGetter defaultActionGetter,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService
        )
    {
        _tgBot = tgBot;
        _contactGetterRepository = contactGetterRepository;
        _defaultActionGetter = defaultActionGetter;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _resourceService = resourceService;
    }

    public void ProcessDefaultSendAction(ITelegramBotClient botClient, long chatId, Message statusMessage, string defaultAction,
                                        CancellationToken cancellationToken, int userId, int defaultCondition, CancellationTokenSource timeoutCTS,
                                        string link, string text)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Резервируем слот, чтобы несколько авто‑действий не стартовали одновременно
                var wait = _tgBot.ReserveDelaySlot(chatId, TimeSpan.FromSeconds(defaultCondition));
                await Task.Delay(wait, timeoutCTS.Token);

                List<long> targetUserIds = new List<long>();
                List<long> mutedByUserIds = new List<long>();
                List<int> userIds = new List<int>();

                int actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        mutedByUserIds = await _userGetter.GetUsersIdForMuteContactIdAsync(userId);

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

                if (TGBot.StateManager.TryGet(chatId, out var state) && state is ProcessVideoDC cfg)
                {
                    // Если к этому моменту пользователь уже начал ручную сессию — не дублируем авто‑отправку
                    if (cfg.IsSessionActiveForMessage(statusMessage.MessageId))
                    {
                        return;
                    }
                    // Берём актуальный текст из pending при автозапуске
                    var effectiveText = cfg.GetPendingTextOrCurrent(statusMessage.MessageId);
                    _ = _tgBot.HandleMediaRequest(botClient, link, chatId, statusMessage, targetUserIds, caption: effectiveText);
                }
            }
            catch (TaskCanceledException) { }
        }, cancellationToken);
    }
}