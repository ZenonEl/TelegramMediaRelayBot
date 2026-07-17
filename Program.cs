// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

global using Serilog;
global using Telegram.Bot;
global using Telegram.Bot.Types;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot;


namespace TelegramMediaRelayBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("============================================");
            Console.WriteLine("TelegramMediaRelayBot");
            Console.WriteLine("Copyright (C) 2024-2025 ZenonEl");
            Console.WriteLine("This program is free software: you can redistribute it and/or modify");
            Console.WriteLine("it under the terms of the GNU Affero General Public License as published");
            Console.WriteLine("by the Free Software Foundation, either version 3 of the License, or");
            Console.WriteLine("(at your option) any later version.");
            Console.WriteLine("Source code: https://github.com/ZenonEl/TelegramMediaRelayBot");
            Console.WriteLine("============================================\n");

            Config.LoadConfig();
            Localization.SetCulture(Config.language);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Config.logLevel)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message} {Exception}{NewLine}").CreateLogger();

            try
            {
                var builder = FluentDBMigrator.CreateAppBuilder(args);

                ServiceProvider serviceProvider = FluentDBMigrator.GetCurrentServiceProvider();
                using (var scope = serviceProvider.CreateScope())
                {
                    var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                    migrator.MigrateUp();
                }

                using var host = builder.Build();
                Scheduler scheduler = host.Services.GetRequiredService<Scheduler>();

                Log.Information($"Log level: {Config.logLevel}");
                DownloadQueue.Initialize(Config.maxConcurrentDownloads);
                scheduler.Init();

                if (Environment.GetEnvironmentVariable("CI") == "true")
                {
                    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(118000); // 1 минута 58 секунд
                        Log.Information("CI build: exiting gracefully after test delay.");
                        lifetime.StopApplication();
                    });
                }

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(Main));
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
