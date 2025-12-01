// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

global using Serilog;
global using Telegram.Bot;
global using Telegram.Bot.Types;
global using TelegramMediaRelayBot.Config.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TelegramMediaRelayBot.Extensions;


namespace TelegramMediaRelayBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        IHostBuilder builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddYamlFile("downloader-config.yaml", optional: false, reloadOnChange: true);
        });

        builder.ConfigureServices((hostContext, services) =>
        {
            services.AddConfigurationServices(hostContext.Configuration);
            services.AddApplicationCore();
            services.AddPersistenceServices(hostContext.Configuration);
        })
        .AddSerilogLogging();

        IHost host = builder.Build();

        await host.ApplyDatabaseMigrationsAsync();

        await host.RunAsync();
    }
}
