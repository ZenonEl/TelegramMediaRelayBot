// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IPrivacySettingsTargetsRepository
{
    // Методы из Setter'а
    Task<int> UpsertTarget(int userId, int privacySettingId, string targetType, string targetValue);
    Task<int> RemoveTarget(int privacySettingId, string targetValue);

    // Методы из Getter'а
    Task<bool> CheckTargetExists(int userId, string type);
    Task<List<string>> GetAllUserTargets(int userId);
}