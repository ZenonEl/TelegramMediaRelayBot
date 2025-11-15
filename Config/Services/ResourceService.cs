// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Resources;

namespace TelegramMediaRelayBot.Config.Services;

/// <summary>
/// Service for accessing localized resource strings using ResourceManager
/// </summary>
public class ResourceService : IResourceService
{
    private readonly ResourceManager _resourceManager;

    public ResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    }

    /// <inheritdoc />
    [Obsolete("Используйте специализированные сервисы, например IUiResourceService")]
    public string GetResourceString(string key)
    {
        return _resourceManager.GetString(key) ?? key;
    }
} 