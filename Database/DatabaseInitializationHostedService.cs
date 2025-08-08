// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using FluentMigrator.Runner;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            // Run migrations
            migrator.MigrateUp();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run database migrations at startup");
            throw;
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}