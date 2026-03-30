// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.


using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;


public class SendToAllContactsSessionCommand : IBotCallbackQueryHandlers
{
    private readonly TGBot _tgBot;
    private readonly IContactGetter _contactGetterRepository;
    private readonly IUserGetter _userGetter;

    public SendToAllContactsSessionCommand(
        TGBot tgBot,
        IContactGetter contactGetterRepository,
        IUserGetter userGetter)
    {
        _tgBot = tgBot;
        _contactGetterRepository = contactGetterRepository;
        _userGetter = userGetter;
    }

    public string Name => "send_to_all_contacts:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string sessionId = update.CallbackQuery!.Data!.Split(':')[1];
        if (!MediaSessionManager.TryGet(sessionId, out var session) || session == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, Config.GetResourceString("SessionExpiredMessage"), showAlert: true);
            return;
        }

        // Cancel default action timeout
        try { session.Cts.Cancel(); } catch (ObjectDisposedException) { }

        long chatId = session.ChatId;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);
        List<long> contactUserTGIds = await _contactGetterRepository.GetAllContactUserTGIds(userId);
        List<long> targetUserIds = contactUserTGIds.Except(mutedByUserIds).ToList();

        int messageId = int.Parse(sessionId);
        var statusMessage = update.CallbackQuery!.Message!;

        MediaSessionManager.Remove(sessionId);

        await botClient.EditMessageText(
            chatId,
            messageId,
            Config.GetResourceString("WaitDownloadingVideo"),
            cancellationToken: ct
        );

        _ = _tgBot.HandleMediaRequest(botClient, session.Url, chatId, statusMessage, targetUserIds, caption: session.Caption ?? "");
    }
}

public class SendToDefaultGroupsSessionCommand : IBotCallbackQueryHandlers
{
    private readonly TGBot _tgBot;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;

    public SendToDefaultGroupsSessionCommand(
        TGBot tgBot,
        IUserGetter userGetter,
        IGroupGetter groupGetter)
    {
        _tgBot = tgBot;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
    }

    public string Name => "send_to_default_groups:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string sessionId = update.CallbackQuery!.Data!.Split(':')[1];
        if (!MediaSessionManager.TryGet(sessionId, out var session) || session == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, Config.GetResourceString("SessionExpiredMessage"), showAlert: true);
            return;
        }

        try { session.Cts.Cancel(); } catch (ObjectDisposedException) { }

        long chatId = session.ChatId;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);
        List<int> userIds = await _groupGetter.GetAllUsersInDefaultEnabledGroups(userId);

        List<long> targetUserIds = userIds
            .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
            .Select(_userGetter.GetTelegramIDbyUserID)
            .ToList();

        int messageId = int.Parse(sessionId);
        var statusMessage = update.CallbackQuery!.Message!;

        MediaSessionManager.Remove(sessionId);

        await botClient.EditMessageText(
            chatId,
            messageId,
            Config.GetResourceString("WaitDownloadingVideo"),
            cancellationToken: ct
        );

        _ = _tgBot.HandleMediaRequest(botClient, session.Url, chatId, statusMessage, targetUserIds, caption: session.Caption ?? "");
    }
}

public class SendOnlyToMeSessionCommand : IBotCallbackQueryHandlers
{
    private readonly TGBot _tgBot;

    public SendOnlyToMeSessionCommand(TGBot tgBot)
    {
        _tgBot = tgBot;
    }

    public string Name => "send_only_to_me:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string sessionId = update.CallbackQuery!.Data!.Split(':')[1];
        if (!MediaSessionManager.TryGet(sessionId, out var session) || session == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, Config.GetResourceString("SessionExpiredMessage"), showAlert: true);
            return;
        }

        try { session.Cts.Cancel(); } catch (ObjectDisposedException) { }

        long chatId = session.ChatId;
        int messageId = int.Parse(sessionId);
        var statusMessage = update.CallbackQuery!.Message!;

        MediaSessionManager.Remove(sessionId);

        await botClient.EditMessageText(
            chatId,
            messageId,
            Config.GetResourceString("WaitDownloadingVideo"),
            cancellationToken: ct
        );

        _ = _tgBot.HandleMediaRequest(botClient, session.Url, chatId, statusMessage, caption: session.Caption ?? "");
    }
}

public class SendToSpecifiedGroupsSessionCommand : IBotCallbackQueryHandlers
{
    private readonly TGBot _tgBot;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;

    public SendToSpecifiedGroupsSessionCommand(
        TGBot tgBot,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        IDefaultActionGetter defaultActionGetter)
    {
        _tgBot = tgBot;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _defaultActionGetter = defaultActionGetter;
    }

    public string Name => "send_to_specified_groups:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string sessionId = update.CallbackQuery!.Data!.Split(':')[1];
        if (!MediaSessionManager.TryGet(sessionId, out var session) || session == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, Config.GetResourceString("SessionExpiredMessage"), showAlert: true);
            return;
        }

        try { session.Cts.Cancel(); } catch (ObjectDisposedException) { }

        long chatId = session.ChatId;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);
        int actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        List<int> groupIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
        List<int> userIds = new List<int>();

        foreach (int groupId in groupIds)
        {
            userIds.AddRange(await _groupGetter.GetAllUsersIdsInGroup(groupId));
        }

        List<long> targetUserIds = userIds
            .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
            .Select(_userGetter.GetTelegramIDbyUserID)
            .ToList();

        int messageId = int.Parse(sessionId);
        var statusMessage = update.CallbackQuery!.Message!;

        MediaSessionManager.Remove(sessionId);

        await botClient.EditMessageText(
            chatId,
            messageId,
            Config.GetResourceString("WaitDownloadingVideo"),
            cancellationToken: ct
        );

        _ = _tgBot.HandleMediaRequest(botClient, session.Url, chatId, statusMessage, targetUserIds, caption: session.Caption ?? "");
    }
}

public class SendToSpecifiedUsersSessionCommand : IBotCallbackQueryHandlers
{
    private readonly TGBot _tgBot;
    private readonly IUserGetter _userGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;

    public SendToSpecifiedUsersSessionCommand(
        TGBot tgBot,
        IUserGetter userGetter,
        IDefaultActionGetter defaultActionGetter)
    {
        _tgBot = tgBot;
        _userGetter = userGetter;
        _defaultActionGetter = defaultActionGetter;
    }

    public string Name => "send_to_specified_users:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string sessionId = update.CallbackQuery!.Data!.Split(':')[1];
        if (!MediaSessionManager.TryGet(sessionId, out var session) || session == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, Config.GetResourceString("SessionExpiredMessage"), showAlert: true);
            return;
        }

        try { session.Cts.Cancel(); } catch (ObjectDisposedException) { }

        long chatId = session.ChatId;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        List<long> mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);
        int actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
        List<int> userIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);

        List<long> targetUserIds = userIds
            .Where(contactId => !mutedByUserIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
            .Select(_userGetter.GetTelegramIDbyUserID)
            .ToList();

        int messageId = int.Parse(sessionId);
        var statusMessage = update.CallbackQuery!.Message!;

        MediaSessionManager.Remove(sessionId);

        await botClient.EditMessageText(
            chatId,
            messageId,
            Config.GetResourceString("WaitDownloadingVideo"),
            cancellationToken: ct
        );

        _ = _tgBot.HandleMediaRequest(botClient, session.Url, chatId, statusMessage, targetUserIds, caption: session.Caption ?? "");
    }
}

public class CancelMediaSessionCommand : IBotCallbackQueryHandlers
{
    public string Name => "cancel_media:";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string sessionId = update.CallbackQuery!.Data!.Split(':')[1];
        if (!MediaSessionManager.TryGet(sessionId, out var session) || session == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, Config.GetResourceString("SessionExpiredMessage"), showAlert: true);
            return;
        }

        long chatId = session.ChatId;
        int messageId = int.Parse(sessionId);

        MediaSessionManager.Remove(sessionId);

        await botClient.EditMessageText(
            chatId,
            messageId,
            Config.GetResourceString("CancelledMessage"),
            cancellationToken: ct
        );
    }
}
