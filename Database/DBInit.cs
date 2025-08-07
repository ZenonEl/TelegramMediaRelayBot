// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using FluentMigrator.Runner;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database.Repositories.MySql;
using TelegramMediaRelayBot.Database.Repositories.Sqlite;
using TelegramMediaRelayBot.TelegramBot;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.TelegramBot.SiteFilter;
using TelegramMediaRelayBot.Config;
using Microsoft.Extensions.Options;


namespace TelegramMediaRelayBot.Database;


public class FluentDBMigrator
{
    private static string GetConnectionString(string dbType, IConfiguration configuration)
    {
        // Get connection string from configuration
        var connectionString = configuration["AppSettings:SqlConnectionString"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }
        
        // Fallback connection strings
        switch (dbType.ToLower())
        {
            case "mysql":
                return "Server=localhost;Database=TelegramMediaRelayBot;Uid=root;Pwd=;";
            default:
                return "Data Source=TelegramMediaRelayBot.db";
        }
    }
    
    public static ServiceProvider GetCurrentServiceProvider(string DBType, IConfiguration configuration)
    {
        var serviceCollection = new ServiceCollection()
            .AddFluentMigratorCore()
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        // Get connection string from configuration
        var connectionString = GetConnectionString(DBType, configuration);
        
        switch (DBType.ToLower())
        {
            case "mysql":
                serviceCollection.ConfigureRunner(rb => rb
                    .AddMySql5()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(MySQLDBMigration).Assembly).For.Migrations());
                break;
                
            default:
                serviceCollection.ConfigureRunner(rb => rb
                    .AddSQLite()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(SQLiteDBMigration).Assembly).For.Migrations());
                break;
        }

        return serviceCollection.BuildServiceProvider(false);
    }

    public static WebApplicationBuilder CreateBuilderByDBType(string[] args, string DBType)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Добавляем конфигурацию загрузчиков
        var downloaderConfigPath = builder.Configuration.GetValue<string>("AppSettings:DownloaderSettings:ConfigFilePath");
        if (!string.IsNullOrEmpty(downloaderConfigPath) && System.IO.File.Exists(downloaderConfigPath))
        {
            builder.Configuration.AddJsonFile(downloaderConfigPath, optional: false, reloadOnChange: true);
            Log.Information("Loaded downloader config from: {Path}", downloaderConfigPath);
        }
        else
        {
            Log.Warning("Downloader config not found at: {Path}", downloaderConfigPath);
        }
        
        // Регистрируем ITelegramBotClient с токеном из конфигурации
        builder.Services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var botConfig = provider.GetRequiredService<IOptions<BotConfiguration>>();
            return new TelegramBotClient(botConfig.Value.TelegramBotToken);
        });

        builder.Services.AddSingleton<ILinkCategorizer>(_ =>
                    new HashTableLinkCategorizer(new DomainsLoader()));

        builder.Services.AddSingleton<CallbackQueryHandlersFactory>();
        builder.Services.AddSingleton<TGBot>();
        builder.Services.AddSingleton<Scheduler>();
        
        // Configure new configuration services
        builder.Services.Configure<BotConfiguration>(
            builder.Configuration.GetSection("AppSettings"));
        builder.Services.Configure<MessageDelayConfiguration>(
            builder.Configuration.GetSection("MessageDelaySettings"));
        builder.Services.Configure<LoggingConfiguration>(
            builder.Configuration.GetSection("ConsoleOutputSettings"));
        builder.Services.Configure<TorConfiguration>(
            builder.Configuration.GetSection("Tor"));
        builder.Services.Configure<AccessPolicyConfiguration>(
            builder.Configuration.GetSection("AccessPolicy"));
        
        // Register configuration services
        builder.Services.AddSingleton<TelegramMediaRelayBot.Config.Services.IConfigurationService, TelegramMediaRelayBot.Config.Services.ConfigurationService>();
        builder.Services.AddSingleton<TelegramMediaRelayBot.Config.Services.IDatabaseConfigurationService, TelegramMediaRelayBot.Config.Services.DatabaseConfigurationService>();
        builder.Services.AddSingleton<TelegramMediaRelayBot.Config.Services.IResourceService, TelegramMediaRelayBot.Config.Services.ResourceService>();
        
        // Регистрация загрузчиков
        builder.Services.AddScoped<TelegramMediaRelayBot.Domain.Interfaces.IMediaDownloaderFactory, TelegramMediaRelayBot.Infrastructure.Factories.MediaDownloaderFactory>();
        builder.Services.AddScoped<TelegramMediaRelayBot.MediaDownloaderService>();

        // Автоматическая регистрация всех загрузчиков
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<IBotCallbackQueryHandlers>()
            .AddClasses(classes => classes.AssignableTo<IBotCallbackQueryHandlers>())
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        // Автоматическая регистрация загрузчиков
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<TelegramMediaRelayBot.Domain.Interfaces.IMediaDownloader>()
            .AddClasses(classes => classes.AssignableTo<TelegramMediaRelayBot.Domain.Interfaces.IMediaDownloader>())
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );



        var connectionString = GetConnectionString(DBType, builder.Configuration);
        
        switch (DBType.ToLower())
        {
            case "mysql":
                MySqlDBCreator.CreateDatabase(connectionString);
                builder.Services.AddSingleton<IUserRepository>(_ =>
                    new MySqlUserRepository(connectionString));
                builder.Services.AddSingleton<IUserGetter>(_ =>
                    new MySqlUserGetter(connectionString));

                builder.Services.AddSingleton<IContactGroupRepository>(_ =>
                    new MySqlContactGroupRepository(connectionString));

                builder.Services.AddSingleton<IContactAdder>(_ =>
                    new MySqlContactAdder(connectionString));
                builder.Services.AddSingleton<IContactGetter>(_ =>
                    new MySqlContactGetter(connectionString));
                builder.Services.AddSingleton<IContactSetter>(_ =>
                    new MySqlContactSetter(connectionString));
                builder.Services.AddSingleton<IContactRemover>(_ =>
                    new MySqlContactRemover(connectionString));

                builder.Services.AddSingleton<IOutboundDBGetter>(_ =>
                    new MySqlOutboundDBGetter(connectionString));
                builder.Services.AddSingleton<IInboundDBGetter>(_ =>
                    new MySqlInboundDBGetter(connectionString));

                builder.Services.AddSingleton<IPrivacySettingsSetter>(_ =>
                    new MySqlPrivacySettingsSetter(connectionString));
                builder.Services.AddSingleton<IPrivacySettingsGetter>(_ =>
                    new MySqlPrivacySettingsGetter(connectionString));
                builder.Services.AddSingleton<IPrivacySettingsTargetsSetter>(_ =>
                    new MySqlPrivacySettingsTargetsSetter(connectionString));
                builder.Services.AddSingleton<IPrivacySettingsTargetsGetter>(_ =>
                    new MySqlPrivacySettingsTargetsGetter(connectionString));

                builder.Services.AddSingleton<IDefaultAction>(_ =>
                    new MySqlDefaultAction(connectionString));
                builder.Services.AddSingleton<IDefaultActionSetter>(_ =>
                    new MySqlDefaultActionSetter(connectionString));
                builder.Services.AddSingleton<IDefaultActionGetter>(_ =>
                    new MySqlDefaultActionGetter(connectionString));

                builder.Services.AddSingleton<IGroupGetter>(_ =>
                    new MySqlGroupGetter(connectionString));
                builder.Services.AddSingleton<IGroupSetter>(_ =>
                    new MySqlGroupSetter(connectionString));

                return builder;
            default:
                builder.Services.AddSingleton<IUserRepository>(_ =>
                    new SqliteUserRepository(connectionString));
                builder.Services.AddSingleton<IUserGetter>(_ =>
                    new SqliteUserGetter(connectionString));

                builder.Services.AddSingleton<IContactGroupRepository>(_ =>
                    new SqliteContactGroupRepository(connectionString));
                builder.Services.AddSingleton<IContactAdder>(_ =>
                    new SqliteContactAdder(connectionString));
                builder.Services.AddSingleton<IContactGetter>(_ =>
                    new SqliteContactGetter(connectionString));
                builder.Services.AddSingleton<IContactSetter>(_ =>
                    new SqliteContactSetter(connectionString));
                builder.Services.AddSingleton<IContactRemover>(_ =>
                    new SqliteContactRemover(connectionString));

                builder.Services.AddSingleton<IOutboundDBGetter>(_ =>
                    new SqliteOutboundDBGetter(connectionString));
                builder.Services.AddSingleton<IInboundDBGetter>(_ =>
                    new SqliteInboundDBGetter(connectionString));

                builder.Services.AddSingleton<IPrivacySettingsSetter>(_ =>
                    new SqlitePrivacySettingsSetter(connectionString));
                builder.Services.AddSingleton<IPrivacySettingsGetter>(_ =>
                    new SqlitePrivacySettingsGetter(connectionString));
                builder.Services.AddSingleton<IPrivacySettingsTargetsSetter>(_ =>
                    new SqlitePrivacySettingsTargetsSetter(connectionString));
                builder.Services.AddSingleton<IPrivacySettingsTargetsGetter>(_ =>
                    new SqlitePrivacySettingsTargetsGetter(connectionString));

                builder.Services.AddSingleton<IDefaultAction>(_ =>
                    new SqliteDefaultAction(connectionString));
                builder.Services.AddSingleton<IDefaultActionSetter>(_ =>
                    new SqliteDefaultActionSetter(connectionString));
                builder.Services.AddSingleton<IDefaultActionGetter>(_ =>
                    new SqliteDefaultActionGetter(connectionString));

                builder.Services.AddSingleton<IGroupGetter>(_ =>
                    new SqliteGroupGetter(connectionString));
                builder.Services.AddSingleton<IGroupSetter>(_ =>
                    new SqliteGroupSetter(connectionString));
                return builder;
        }
    }
}