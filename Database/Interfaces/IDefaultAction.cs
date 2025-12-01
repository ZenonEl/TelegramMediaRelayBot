// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database.Interfaces;

public interface IDefaultAction
{
    public Task<bool> AddDefaultUsersActionTargets(int userId, int actionId, string targetType, int targetId);

    public Task<bool> RemoveAllDefaultUsersActionTargets(int userId, string targetType, int actionId);
}

public interface IDefaultActionSetter
{
    public Task<bool> SetAutoSendVideoConditionToUser(int userId, string actionCondition, string type);

    public Task<bool> SetAutoSendVideoActionToUser(int userId, string action, string type);

}

public interface IDefaultActionGetter
{
    public List<int> GetAllDefaultUsersActionTargets(int userId, string targetType, int actionId);

    public int GetDefaultActionId(int userId, string type);
    public string GetDefaultActionByUserIDAndType(int userID, string type);

    // Async versions
    public Task<List<int>> GetAllDefaultUsersActionTargetsAsync(int userId, string targetType, int actionId);
    public Task<int> GetDefaultActionIdAsync(int userId, string type);
    public Task<string> GetDefaultActionByUserIDAndTypeAsync(int userID, string type);
}
