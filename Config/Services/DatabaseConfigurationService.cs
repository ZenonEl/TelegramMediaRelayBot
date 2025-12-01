// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

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
