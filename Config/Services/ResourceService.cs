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
    private readonly IUiResourceService _uiResources;

    public ResourceService(
        IUiResourceService uiResources
    )
    {
        _uiResources = uiResources;
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
    }

    /// <inheritdoc />
    [Obsolete("Используйте специализированные сервисы, например IUiResourceService")]
    public string GetResourceString(string key)
    {
        if (_uiResources.TryGetString(key, out string? value))
        {
            return value;
        }
        return _resourceManager.GetString(key) ?? key;
    }
} 

public class UiResourceService : IUiResourceService
{
    private readonly ResourceManager _resourceManager;

    public UiResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.UI", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}