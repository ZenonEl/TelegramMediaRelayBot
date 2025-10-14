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
global using TelegramMediaRelayBot.Config.Services;
using Microsoft.Extensions.Configuration;
using TelegramMediaRelayBot.Extensions;
using Microsoft.Extensions.Hosting;


namespace TelegramMediaRelayBot
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddYamlFile("downloader-config.yaml", optional: false, reloadOnChange: true);
            });

            // Конфигурируем сервисы и логирование
            builder.ConfigureServices((hostContext, services) =>
            {
                // TODO: Здесь будем регистрировать остальные сервисы
                services.AddConfigurationServices(hostContext.Configuration);
                services.AddApplicationCore();
                services.AddPersistenceServices(hostContext.Configuration);
                // services.AddTelegramBot(...);
                // services.AddBackupServices(...);
            })
            .AddSerilogLogging(); // Применяем нашу конфигурацию Serilog

            var host = builder.Build();

            await host.ApplyDatabaseMigrationsAsync();

            await host.RunAsync();
        }
    }
}