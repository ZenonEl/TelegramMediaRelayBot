// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Config.Services;

/// <summary>
/// Service for accessing localized resource strings
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Gets a localized resource string by key
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <returns>Localized string</returns>
    [Obsolete("Используйте специализированные сервисы, например IUiResourceService")]
    string GetResourceString(string key);
}

public interface IUiResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface IErrorsResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface IFormattingResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface IHelpResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface IInboxResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface ISettingsResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface IStatesResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface IStatusResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}

public interface IStatusRuRuResourceService
{
    string GetString(string key);
    bool TryGetString(string key, out string? value);
}
