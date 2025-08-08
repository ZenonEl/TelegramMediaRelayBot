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
using System.Globalization;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.TelegramBot;
using TelegramMediaRelayBot.Config;


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

            // Создаем единую конфигурацию
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.example.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Получаем настройки языка
            var language = configuration.GetValue<string>("AppSettings:Language", "en-US");
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;

            currentCulture = new CultureInfo(language);

            Thread.CurrentThread.CurrentUICulture = currentCulture;
            Thread.CurrentThread.CurrentCulture = currentCulture;

            // Получаем уровень логирования (горячо через switch)
            var initialLevel = configuration.GetValue<Serilog.Events.LogEventLevel>("ConsoleOutputSettings:LogLevel", Serilog.Events.LogEventLevel.Information);
            var levelSwitch = new Serilog.Core.LoggingLevelSwitch(initialLevel);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message} {Exception}{NewLine}").CreateLogger();

            try
            {
                var builder = FluentDBMigrator.CreateBuilderByDBType(args, configuration.GetValue<string>("AppSettings:DatabaseType", "sqlite"), configuration);

                // Run DB migrations before starting services
                ServiceProvider serviceProvider = FluentDBMigrator.GetCurrentServiceProvider(
                    configuration.GetValue<string>("AppSettings:DatabaseType", "sqlite"), 
                    configuration);
                using (var scope = serviceProvider.CreateScope())
                {
                    var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                    migrator.MigrateUp();
                }

                var app = builder.Build();
                // Подписка на горячую смену уровня логов
                var loggingMonitor = app.Services.GetRequiredService<IOptionsMonitor<LoggingConfiguration>>();
                loggingMonitor.OnChange(cfg =>
                {
                    if (levelSwitch.MinimumLevel == cfg.LogLevel)
                    {
                        return; // avoid duplicate messages on equivalent reloads
                    }
                    levelSwitch.MinimumLevel = cfg.LogLevel;
                    Log.Information("Applied hot config [ConsoleOutputSettings]: LogLevel -> {Level}", cfg.LogLevel);
                });
                // ensure our change logger is constructed (subscribes to OnChange in ctor)
                _ = app.Services.GetRequiredService<TelegramMediaRelayBot.Config.Services.ConfigurationChangeLogger>();
                TGBot tgBot = app.Services.GetRequiredService<TGBot>();
                Scheduler scheduler = app.Services.GetRequiredService<Scheduler>();

                Log.Information($"Log level: {initialLevel}");
                scheduler.Init();

                await tgBot.Start();
                await Task.Delay(-1);
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
