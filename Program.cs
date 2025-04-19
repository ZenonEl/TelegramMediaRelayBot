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
global using DataBase;
global using Telegram.Bot;
global using Telegram.Bot.Types;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.Database;
using Microsoft.AspNetCore.Builder;
using TelegramMediaRelayBot;
using TelegramMediaRelayBot.Database.Repositories.MySql;
using TelegramMediaRelayBot.Database.Interfaces;



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

            CultureInfo currentCulture = CultureInfo.CurrentUICulture;

            if (Config.language != null)
            {
                currentCulture = new CultureInfo(Config.language);
            }

            Thread.CurrentThread.CurrentUICulture = currentCulture;
            Thread.CurrentThread.CurrentCulture = currentCulture;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Config.logLevel)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message} {Exception}{NewLine}").CreateLogger();

            try
            {

                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddSingleton<ITelegramBotClient>(_ => 
                    new TelegramBotClient(Config.telegramBotToken!));
                builder.Services.AddSingleton<IUserRepository>(_ => 
                    new UserRepository(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IUserGettersRepository>(_ => 
                    new UserGettersRepository(Config.sqlConnectionString!));

                builder.Services.AddSingleton<TGBot>();
                var app = builder.Build();
                Config.bot = app.Services.GetRequiredService<TGBot>();

                Log.Information($"Log level: {Config.logLevel}");
                CoreDB.InitDB();
                Scheduler.Scheduler.Init();

                await Config.bot.Start();

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
