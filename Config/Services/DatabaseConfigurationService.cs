// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.Config.Services;

/// <summary>
/// Service for accessing database configuration using IOptions pattern
/// </summary>
public class DatabaseConfigurationService : IDatabaseConfigurationService
{
    private readonly IOptions<BotConfiguration> _botConfig;

    public DatabaseConfigurationService(IOptions<BotConfiguration> botConfig)
    {
        _botConfig = botConfig;
    }

    /// <inheritdoc />
    public string GetConnectionString()
    {
        return _botConfig.Value.SqlConnectionString;
    }

    /// <inheritdoc />
    public string GetDatabaseType()
    {
        return _botConfig.Value.DatabaseType.ToLowerInvariant();
    }

    /// <inheritdoc />
    public string GetDatabaseName()
    {
        return _botConfig.Value.DatabaseName;
    }
} 