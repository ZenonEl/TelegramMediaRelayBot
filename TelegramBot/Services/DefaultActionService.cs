// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Sessions;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Database;
using TelegramBot.Services;
using Microsoft.Extensions.Options;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IDefaultActionService
{
    Task ProcessDefaultSendAction(ITelegramBotClient botClient, DownloadSession session, CancellationToken cancellationToken);
}

public class DefaultActionService : IDefaultActionService
{
    private readonly IContactGetter _contactGetter;
    private readonly IDefaultActionGetter _defaultActionGetter;
    private readonly IUserGetter _userGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IUserRequestThrottler _throttler;
    private readonly MediaDownloaderService _downloaderService;
    private readonly Infrastructure.MediaProcessing.IMediaProcessingService _mediaProcessingService;
    private readonly IOptionsMonitor<Config.DownloadingConfiguration> _downloadingConfig;

    public DefaultActionService(
        IContactGetter contactGetter,
        IDefaultActionGetter defaultActionGetter,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        IUserRequestThrottler throttler,
        MediaDownloaderService downloaderService,
        Infrastructure.MediaProcessing.IMediaProcessingService mediaProcessingService,
        IOptionsMonitor<Config.DownloadingConfiguration> downloadingConfig
        )
    {
        _contactGetter = contactGetter;
        _defaultActionGetter = defaultActionGetter;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _throttler = throttler;
        _downloaderService = downloaderService;
        _mediaProcessingService = mediaProcessingService;
        _downloadingConfig = downloadingConfig;
    }

    public async Task ProcessDefaultSendAction(ITelegramBotClient botClient, DownloadSession session, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _userGetter.GetUserIDbyTelegramID(session.ChatId);
            var defaultActionData = _defaultActionGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
            if (defaultActionData == UsersAction.NO_VALUE) return;

            var parts = defaultActionData.Split(';');
            var defaultAction = parts[0];
            if (parts.Length < 2 || !int.TryParse(parts[1], out var delaySeconds)) delaySeconds = 5;

            // Используем наш новый сервис для очереди
            var wait = _throttler.ReserveSlot(session.ChatId, TimeSpan.FromSeconds(delaySeconds));
            await Task.Delay(wait, cancellationToken);
            
            // Получаем цели рассылки
            var targetUserIds = await GetTargetUserIds(userId, defaultAction);

            // Запускаем загрузку через MediaDownloaderService
            var downloadOptions = new DownloadOptions(); // Пока пустые опции
            var downloadResult = await _downloaderService.DownloadMedia(session.Url, downloadOptions, cancellationToken);

            if (downloadResult.Success)
            {
                var processedFiles = await _mediaProcessingService.ApplySizePolicyAsync(downloadResult.MediaFiles, _downloadingConfig.CurrentValue, cancellationToken);
                // TODO: Вызвать SendMediaToTelegram, который мы тоже вынесем в отдельный сервис
            }
            else
            {
                 // Обработка ошибки скачивания
            }
        }
        catch (TaskCanceledException) { /* Игнорируем отмену */ }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ProcessDefaultSendAction for message {MessageId}", session.StatusMessageId);
        }
    }

    private async Task<List<long>> GetTargetUserIds(int userId, string defaultAction)
    {
        var mutedByUserIds = _userGetter.GetUsersIdForMuteContactId(userId);
        var actionId = _defaultActionGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);

        List<int> userIds = new();
        
        switch (defaultAction)
        {
            case UsersAction.SEND_MEDIA_TO_ALL_CONTACTS:
                var tgIds = await _contactGetter.GetAllContactUserTGIds(userId);
                return tgIds.Except(mutedByUserIds).ToList();

            case UsersAction.SEND_MEDIA_TO_DEFAULT_GROUPS:
                userIds = await _groupGetter.GetAllUsersInDefaultEnabledGroups(userId);
                break;

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_GROUPS:
                var groupIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
                foreach (var groupId in groupIds) userIds.AddRange(await _groupGetter.GetAllUsersIdsInGroup(groupId));
                break;

            case UsersAction.SEND_MEDIA_TO_SPECIFIED_USERS:
                userIds = _defaultActionGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
                break;
        }

        return userIds
            .Select(_userGetter.GetTelegramIDbyUserID)
            .Where(tgId => !mutedByUserIds.Contains(tgId))
            .ToList();
    }
}