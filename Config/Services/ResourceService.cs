// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

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

public class ErrorsResourceService : IErrorsResourceService
{
    private readonly ResourceManager _resourceManager;

    public ErrorsResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.Errors", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}

public class FormattingResourceService : IFormattingResourceService
{
    private readonly ResourceManager _resourceManager;

    public FormattingResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.Formatting", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}

public class HelpResourceService : IHelpResourceService
{
    private readonly ResourceManager _resourceManager;

    public HelpResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.Help", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}

public class InboxResourceService : IInboxResourceService
{
    private readonly ResourceManager _resourceManager;

    public InboxResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.Inbox", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}

public class SettingsResourceService : ISettingsResourceService
{
    private readonly ResourceManager _resourceManager;

    public SettingsResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.Settings", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}

public class StatesResourceService : IStatesResourceService
{
    private readonly ResourceManager _resourceManager;

    public StatesResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.States", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}

public class StatusResourceService : IStatusResourceService
{
    private readonly ResourceManager _resourceManager;

    public StatusResourceService()
    {
        _resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.Status", typeof(Program).Assembly);
    }

    public string GetString(string key) => _resourceManager.GetString(key) ?? $"[[{key}]]";

    public bool TryGetString(string key, out string? value)
    {
        value = _resourceManager.GetString(key);
        return value != null;
    }
}
