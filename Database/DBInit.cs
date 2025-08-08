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
using FluentMigrator.Runner.Initialization;
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
        static bool LooksLikeMySql(string cs) =>
            cs.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            cs.Contains("Uid=", StringComparison.OrdinalIgnoreCase) ||
            cs.Contains("User Id=", StringComparison.OrdinalIgnoreCase);

        static bool LooksLikeSqlite(string cs) =>
            cs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
            cs.Contains("Filename=", StringComparison.OrdinalIgnoreCase) ||
            cs.Trim().EndsWith(".db", StringComparison.OrdinalIgnoreCase);

        static string EnsureSqliteFormat(string cs)
        {
            var trimmed = cs.Trim();
            if (trimmed.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }
            // If it's a bare path like "MyBot.db" – convert to Data Source=
            if (trimmed.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                var fullPath = System.IO.Path.IsPathRooted(trimmed)
                    ? trimmed
                    : System.IO.Path.Combine(AppContext.BaseDirectory, trimmed);
                return $"Data Source={fullPath}";
            }
            return trimmed;
        }

        var raw = configuration["AppSettings:SqlConnectionString"] ?? string.Empty;
        var type = (dbType ?? "sqlite").ToLowerInvariant();

        switch (type)
        {
            case "mysql":
                if (!string.IsNullOrWhiteSpace(raw) && LooksLikeMySql(raw))
                {
                    return raw;
                }
                return "Server=localhost;Database=TelegramMediaRelayBot;Uid=root;Pwd=;";
            default: // sqlite
                if (!string.IsNullOrWhiteSpace(raw) && LooksLikeSqlite(raw))
                {
                    return EnsureSqliteFormat(raw);
                }
                return $"Data Source={System.IO.Path.Combine(AppContext.BaseDirectory, "TelegramMediaRelayBot.db")}";
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
                serviceCollection.Configure<RunnerOptions>(opt => opt.Tags = new[] { "mysql" });
                break;
                
            default:
                serviceCollection.ConfigureRunner(rb => rb
                    .AddSQLite()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(SQLiteDBMigration).Assembly).For.Migrations());
                serviceCollection.Configure<RunnerOptions>(opt => opt.Tags = new[] { "sqlite" });
                break;
        }

        return serviceCollection.BuildServiceProvider(false);
    }

    public static WebApplicationBuilder CreateBuilderByDBType(string[] args, string DBType, IConfiguration configuration)
    {
        var builder = WebApplication.CreateBuilder(args);
        var downloaderConfigPath = configuration.GetValue<string>("AppSettings:DownloaderSettings:ConfigFilePath");

        if (!string.IsNullOrEmpty(downloaderConfigPath))
        {
            // Получаем полный путь к файлу конфигурации
            var fullPath = Path.IsPathRooted(downloaderConfigPath) 
                ? downloaderConfigPath 
                : Path.Combine(AppContext.BaseDirectory, downloaderConfigPath);
            
            if (System.IO.File.Exists(fullPath))
            {
                // Добавляем конфигурацию загрузчиков к переданной конфигурации
                var configBuilder = new ConfigurationBuilder()
                    .AddConfiguration(configuration)
                    .AddJsonFile(fullPath, optional: false, reloadOnChange: true);
                
                var finalConfiguration = configBuilder.Build();
                
                // Обновляем сервисы с новой конфигурацией
                builder.Services.Configure<BotConfiguration>(
                    finalConfiguration.GetSection("AppSettings"));
                builder.Services.Configure<MessageDelayConfiguration>(
                    finalConfiguration.GetSection("MessageDelaySettings"));
                builder.Services.Configure<LoggingConfiguration>(
                    finalConfiguration.GetSection("ConsoleOutputSettings"));
                builder.Services.Configure<TorConfiguration>(
                    finalConfiguration.GetSection("Tor"));
                builder.Services.Configure<AccessPolicyConfiguration>(
                    finalConfiguration.GetSection("AccessPolicy"));
                
                // Регистрируем финальную конфигурацию для загрузчиков
                builder.Services.AddSingleton<IConfiguration>(finalConfiguration);
                
                Log.Information("Loaded downloader config from: {Path}", fullPath);
            }
            else
            {
                // Если файл не найден, используем исходную конфигурацию
                builder.Services.AddSingleton<IConfiguration>(configuration);
                Log.Warning("Downloader config not found at: {Path}", fullPath);
            }
        }
        else
        {
            Log.Warning("Downloader config path not specified in configuration");
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
        builder.Services.AddSingleton<IUserStateManager, InMemoryUserStateManager>();
        builder.Services.AddScoped<IUserFilterService, DefaultUserFilterService>();
        builder.Services.AddHostedService<DatabaseInitializationHostedService>();
        
        // Configure new configuration services
        builder.Services.Configure<BotConfiguration>(
            configuration.GetSection("AppSettings"));
        builder.Services.Configure<MessageDelayConfiguration>(
            configuration.GetSection("MessageDelaySettings"));
        builder.Services.Configure<LoggingConfiguration>(
            configuration.GetSection("ConsoleOutputSettings"));
        builder.Services.Configure<TorConfiguration>(
            configuration.GetSection("Tor"));
        builder.Services.Configure<AccessPolicyConfiguration>(
            configuration.GetSection("AccessPolicy"));
        
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
                // Ensure database is created via hosted service at startup
                builder.Services.AddSingleton<MySqlDBCreator>();
                builder.Services.AddScoped<IUserRepository>(_ =>
                    new MySqlUserRepository(connectionString));
                builder.Services.AddScoped<IUserGetter>(_ =>
                    new MySqlUserGetter(connectionString));

                builder.Services.AddScoped<IContactGroupRepository>(_ =>
                    new MySqlContactGroupRepository(connectionString));

                builder.Services.AddScoped<IContactAdder>(_ =>
                    new MySqlContactAdder(connectionString));
                builder.Services.AddScoped<IContactGetter>(_ =>
                    new MySqlContactGetter(connectionString, _.GetRequiredService<TelegramMediaRelayBot.Config.Services.IResourceService>()));
                builder.Services.AddScoped<IContactSetter>(_ =>
                    new MySqlContactSetter(connectionString));
                builder.Services.AddScoped<IContactRemover>(_ =>
                    new MySqlContactRemover(connectionString));

                builder.Services.AddScoped<IOutboundDBGetter>(_ =>
                    new MySqlOutboundDBGetter(connectionString));
                builder.Services.AddScoped<IInboundDBGetter>(_ =>
                    new MySqlInboundDBGetter(connectionString));

                builder.Services.AddScoped<IPrivacySettingsSetter>(_ =>
                    new MySqlPrivacySettingsSetter(connectionString));
                builder.Services.AddScoped<IPrivacySettingsGetter>(_ =>
                    new MySqlPrivacySettingsGetter(connectionString));
                builder.Services.AddScoped<IPrivacySettingsTargetsSetter>(_ =>
                    new MySqlPrivacySettingsTargetsSetter(connectionString));
                builder.Services.AddScoped<IPrivacySettingsTargetsGetter>(_ =>
                    new MySqlPrivacySettingsTargetsGetter(connectionString));

                builder.Services.AddScoped<IDefaultAction>(_ =>
                    new MySqlDefaultAction(connectionString));
                builder.Services.AddScoped<IDefaultActionSetter>(_ =>
                    new MySqlDefaultActionSetter(connectionString));
                builder.Services.AddScoped<IDefaultActionGetter>(_ =>
                    new MySqlDefaultActionGetter(connectionString));

                builder.Services.AddScoped<IGroupGetter>(_ =>
                    new MySqlGroupGetter(connectionString));
                builder.Services.AddScoped<IGroupSetter>(_ =>
                    new MySqlGroupSetter(connectionString));

                return builder;
            default:
                builder.Services.AddScoped<IUserRepository>(_ =>
                    new SqliteUserRepository(connectionString));
                builder.Services.AddScoped<IUserGetter>(_ =>
                    new SqliteUserGetter(connectionString));

                builder.Services.AddScoped<IContactGroupRepository>(_ =>
                    new SqliteContactGroupRepository(connectionString));
                builder.Services.AddScoped<IContactAdder>(_ =>
                    new SqliteContactAdder(connectionString));
                builder.Services.AddScoped<IContactGetter>(_ =>
                    new SqliteContactGetter(connectionString, _.GetRequiredService<TelegramMediaRelayBot.Config.Services.IResourceService>()));
                builder.Services.AddScoped<IContactSetter>(_ =>
                    new SqliteContactSetter(connectionString));
                builder.Services.AddScoped<IContactRemover>(_ =>
                    new SqliteContactRemover(connectionString));

                builder.Services.AddScoped<IOutboundDBGetter>(_ =>
                    new SqliteOutboundDBGetter(connectionString));
                builder.Services.AddScoped<IInboundDBGetter>(_ =>
                    new SqliteInboundDBGetter(connectionString));

                builder.Services.AddScoped<IPrivacySettingsSetter>(_ =>
                    new SqlitePrivacySettingsSetter(connectionString));
                builder.Services.AddScoped<IPrivacySettingsGetter>(_ =>
                    new SqlitePrivacySettingsGetter(connectionString));
                builder.Services.AddScoped<IPrivacySettingsTargetsSetter>(_ =>
                    new SqlitePrivacySettingsTargetsSetter(connectionString));
                builder.Services.AddScoped<IPrivacySettingsTargetsGetter>(_ =>
                    new SqlitePrivacySettingsTargetsGetter(connectionString));

                builder.Services.AddScoped<IDefaultAction>(_ =>
                    new SqliteDefaultAction(connectionString));
                builder.Services.AddScoped<IDefaultActionSetter>(_ =>
                    new SqliteDefaultActionSetter(connectionString));
                builder.Services.AddScoped<IDefaultActionGetter>(_ =>
                    new SqliteDefaultActionGetter(connectionString));

                builder.Services.AddScoped<IGroupGetter>(_ =>
                    new SqliteGroupGetter(connectionString));
                builder.Services.AddScoped<IGroupSetter>(_ =>
                    new SqliteGroupSetter(connectionString));
                return builder;
        }
    }
}