// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.Infrastructure.Backup;

public sealed class BackupProviderFactory : IBackupProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<BackupConfiguration> _settings;

    public BackupProviderFactory(IConfiguration configuration, IOptions<BackupConfiguration> settings)
    {
        _configuration = configuration;
        _settings = settings;
    }

    public IBackupProvider Create(string dbType)
    {
        dbType = (dbType ?? "sqlite").ToLowerInvariant();
        return dbType switch
        {
            "mysql" => new MySqlBackupProvider(_configuration, _settings),
            _ => new SqliteBackupProvider(_configuration, _settings),
        };
    }
}

