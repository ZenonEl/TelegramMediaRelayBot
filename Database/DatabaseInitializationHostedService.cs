// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TelegramMediaRelayBot.Database;

public class DatabaseInitializationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationHostedService> _logger;
    private readonly Repositories.MySql.MySqlDBCreator? _mysqlCreator;

    public DatabaseInitializationHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationHostedService> logger,
        Repositories.MySql.MySqlDBCreator? mysqlCreator = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mysqlCreator = mysqlCreator;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}