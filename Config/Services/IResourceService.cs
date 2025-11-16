// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

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
