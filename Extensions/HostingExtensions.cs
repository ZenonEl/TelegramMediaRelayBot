using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using TelegramMediaRelayBot.Config;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.TelegramBot;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using TelegramMediaRelayBot.TelegramBot.SiteFilter;
using FluentValidation;
using TelegramMediaRelayBot.TelegramBot.Validation;
using TelegramMediaRelayBot.Infrastructure.Backup;
using Microsoft.Extensions.Hosting;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Infrastructure.Factories;
using System.Data;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner;
using TelegramMediaRelayBot.Database.UnitOfWork.Services;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using TelegramMediaRelayBot.Migrations;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database.Repositories.MySql;
using TelegramMediaRelayBot.Database.Repositories.Sqlite;
using Dapper;
using Microsoft.Extensions.Logging;

namespace TelegramMediaRelayBot.Extensions
{
    public static class HostingExtensions
    {
        /// <summary>
        /// Configures Serilog as the logging provider.
        /// </summary>
        public static IHostBuilder AddSerilogLogging(this IHostBuilder builder)
        {
            // Эта строка делает всю магию:
            // 1. Устанавливает Serilog как провайдер логирования.
            // 2. Читает конфигурацию для Serilog прямо из IConfiguration (appsettings.json).
            // 3. Автоматически обеспечивает горячую перезагрузку настроек.
            builder.UseSerilog((hostingContext, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
            });

            return builder;
        }

        /// <summary>
        /// Registers configuration-related services and validators.
        /// </summary>
        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind configuration sections to strongly-typed options classes
            services.Configure<BotConfiguration>(configuration.GetSection("AppSettings"));
            services.Configure<MessageDelayConfiguration>(configuration.GetSection("MessageDelaySettings"));
            services.Configure<LoggingConfiguration>(configuration.GetSection("ConsoleOutputSettings"));
            services.Configure<DownloadingConfiguration>(configuration.GetSection("Downloading"));
            services.Configure<TorConfiguration>(configuration.GetSection("Tor"));
            services.Configure<AccessPolicyConfiguration>(configuration.GetSection("AccessPolicy"));
            services.Configure<TextCleanupConfiguration>(configuration.GetSection("TextCleanup"));
            services.Configure<BackupConfiguration>(configuration.GetSection("Backup"));
            services.Configure<DownloaderConfigRoot>(configuration);

            // Register configuration validators (fail fast on startup)
            services.AddSingleton<IValidateOptions<BotConfiguration>, Config.Validation.BotConfigurationValidator>();
            services.AddSingleton<IValidateOptions<TorConfiguration>, Config.Validation.TorConfigurationValidator>();
            services.AddSingleton<IValidateOptions<DownloaderSettingsConfiguration>, Config.Validation.DownloaderSettingsConfigurationValidator>();

            // Register FluentValidation validators
            services.AddScoped<IValidator<InboxListRequest>, InboxListRequestValidator>();
            services.AddScoped<IValidator<InboxViewRequest>, InboxViewRequestValidator>();

            // Service that logs configuration changes (hot reload)
            services.AddSingleton<Config.Services.ConfigurationChangeLogger>();

            return services;
        }

        /// <summary>
        /// Registers core application services.
        /// </summary>
        public static IServiceCollection AddApplicationCore(this IServiceCollection services)
        {
            // Register Telegram.Bot client with a factory
            services.AddSingleton<ITelegramBotClient>(provider =>
            {
                var botConfig = provider.GetRequiredService<IOptions<BotConfiguration>>();
                if (string.IsNullOrWhiteSpace(botConfig.Value.TelegramBotToken))
                {
                    throw new ArgumentException("Telegram Bot Token is not configured.", nameof(BotConfiguration.TelegramBotToken));
                }
                return new TelegramBotClient(botConfig.Value.TelegramBotToken);
            });

            // Register our main bot class as a Hosted Service
            services.AddHostedService<TGBot>();

            // Other application singletons
            services.AddSingleton<Scheduler>();
            services.AddSingleton<ILinkCategorizer, HashTableLinkCategorizer>();
            services.AddSingleton<IDomainsLoader, DomainsLoader>(); // Assuming DomainsLoader is the implementation
            services.AddSingleton<CallbackQueryHandlersFactory>();
            services.AddSingleton<IUserStateManager, InMemoryUserStateManager>();
            services.AddSingleton<IMediaDownloaderFactory, MediaDownloaderFactory>();

            // Scoped services
            services.AddScoped<IUserFilterService, DefaultUserFilterService>();
            services.AddScoped<MediaDownloaderService>();

            // Register all IBotCallbackQueryHandlers implementations using Scrutor
            services.Scan(scan => scan
                .FromAssemblyOf<IBotCallbackQueryHandlers>()
                .AddClasses(classes => classes.AssignableTo<IBotCallbackQueryHandlers>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            // Other Infrastructure Services
            services.AddSingleton<Config.Services.IConfigurationService, Config.Services.ConfigurationService>();
            services.AddSingleton<Config.Services.IDatabaseConfigurationService, Config.Services.DatabaseConfigurationService>();
            services.AddSingleton<Config.Services.IResourceService, Config.Services.ResourceService>();
            services.AddSingleton<Infrastructure.MediaProcessing.IMediaProcessingService, Infrastructure.MediaProcessing.FfmpegService>();
            services.AddSingleton<TelegramBot.Utils.ITextCleanupService, TelegramBot.Utils.TextCleanupService>();
            services.AddScoped<TelegramBot.Services.IDefaultSummaryService, TelegramBot.Services.DefaultSummaryService>();
            services.AddSingleton<Infrastructure.Processes.IProcessRunner, Infrastructure.Processes.ProcessRunner>();
            services.AddSingleton<Infrastructure.Downloaders.Arguments.IArgumentBuilder, Infrastructure.Downloaders.Arguments.ArgumentBuilder>();
            services.AddSingleton<Infrastructure.Downloaders.Policies.IProxyPolicyManager, Infrastructure.Downloaders.Policies.ProxyPolicyManager>();
            services.AddSingleton<Infrastructure.Downloaders.Policies.IRetryPolicyManager, Infrastructure.Downloaders.Policies.RetryPolicyManager>();

            // Backup Infrastructure
            services.AddSingleton<IBackupProviderFactory, BackupProviderFactory>();
            services.AddSingleton<IBackupOrchestrator, BackupOrchestrator>();
            services.AddHostedService<BackupHostedService>(); // Используем наш новый IHostedService для бэкапов

            services.AddScoped<IContactUoW, ContactUoWService>();
            services.AddScoped<IDefaultActionUoW, DefaultActionUoWService>();
            services.AddScoped<IGroupUoW, GroupUoWService>(); 
            services.AddScoped<IPrivacySettingsUoW, PrivacySettingsUoWService>();
            services.AddScoped<IPrivacySettingsTargetsUoW, PrivacySettingsTargetsUoWService>();

            return services;
        }

        /// <summary>
        /// Registers data persistence services based on the configured database type.
        /// </summary>
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            var dbType = configuration.GetValue<string>("AppSettings:DatabaseType") ?? "sqlite";
            var connectionString = GetConnectionString(dbType, configuration);

            // Вся конфигурация FluentMigrator в одном месте
            services.AddFluentMigratorCore()
                .ConfigureRunner(rb =>
                {
                    // Указываем провайдера БД в зависимости от типа
                    switch (dbType.ToLowerInvariant())
                    {
                        case "mysql":
                            rb.AddMySql5();
                            break;

                        case "sqlite":
                        default:
                            rb.AddSQLite();
                            break;
                    }

                    // Теперь указываем общие для всех провайдеров вещи
                    rb.WithGlobalConnectionString(connectionString)
                        .ScanIn(typeof(M001_CreateInitialSchema).Assembly).For.Migrations();
                })
                .AddLogging(lb => lb.AddFluentMigratorConsole());

            // Delegate to a specific method based on DB type, но уже без конфигурации ранера
            switch (dbType.ToLowerInvariant())
            {
                case "mysql":
                    services.AddMySqlPersistence(connectionString);
                    break;

                case "sqlite":
                default:
                    services.AddSqlitePersistence(connectionString);
                    break;
            }

            // Передаем тег в RunnerOptions, это остается как было
            services.Configure<RunnerOptions>(opt =>
            {
                opt.Tags = new[] { dbType.ToLowerInvariant() };
            });

            return services;
        }

        /// <summary>
        /// Registers services and repositories for SQLite persistence.
        /// </summary>
        private static IServiceCollection AddSqlitePersistence(this IServiceCollection services, string connectionString)
        {
            // Configure FluentMigrator for SQLite
            //services.ConfigureRunner(rb => rb.AddSQLite());

            // Register a transient IDbConnection factory for SQLite
            services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));

            // Register Unit of Work implementation for SQLite
            services.AddScoped<Database.UnitOfWork.IUnitOfWork, Database.UnitOfWork.SqliteUnitOfWork>();

            // ========================================================================
            // == РЕПОЗИТОРИИ (ПРЯМОЙ ДОСТУП К ДАННЫМ)
            // ========================================================================
            // "Глупые" классы, которые только выполняют SQL.
            services.AddScoped<IContactRepository, SqliteContactRepository>();
            services.AddScoped<IDefaultActionRepository, SqliteDefaultActionRepository>();
            services.AddScoped<IDefaultActionTargetsRepository, SqliteDefaultActionTargetsRepository>();
            services.AddScoped<IGroupRepository, SqliteGroupRepository>();
            services.AddScoped<IPrivacySettingsRepository, SqlitePrivacySettingsRepository>();
            services.AddScoped<IPrivacySettingsTargetsRepository, SqlitePrivacySettingsTargetsRepository>();
            // ... Добавь сюда другие новые репозитории по мере их создания ...
            
            // ========================================================================
            // == ФАСАДЫ И GETTER'ы (СТАРЫЕ ИНТЕРФЕЙСЫ)
            // ========================================================================
            // Эти классы реализуют старые интерфейсы, но под капотом вызывают новые UoW сервисы или репозитории.
            // Это обеспечивает обратную совместимость с кодом бота.

            // --- Контакты ---
            services.AddScoped<IContactAdder, SqliteContactAdder>();
            services.AddScoped<IContactRemover, SqliteContactRemover>();
            services.AddScoped<IContactSetter, SqliteContactSetter>();
            services.AddScoped<IContactGetter, SqliteContactGetter>(); // Убедись, что этот класс рефакторен

            // --- Группы ---
            services.AddScoped<IGroupRepository, SqliteGroupRepository>();
            services.AddScoped<IGroupSetter, SqliteGroupSetter>();
            services.AddScoped<IGroupGetter, SqliteGroupGetter>(); // Убедись, что этот класс рефакторен

            // --- Действия по умолчанию ---
            services.AddScoped<IDefaultAction, SqliteDefaultAction>();
            services.AddScoped<IDefaultActionSetter, SqliteDefaultActionSetter>();
            services.AddScoped<IDefaultActionGetter, SqliteDefaultActionGetter>(); // Убедись, что этот класс рефакторен

            // --- Настройки Приватности ---
            services.AddScoped<IPrivacySettingsSetter, SqlitePrivacySettingsSetter>();
            services.AddScoped<IPrivacySettingsGetter, SqlitePrivacySettingsGetter>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IPrivacySettingsTargetsSetter, SqlitePrivacySettingsTargetsSetter>();
            services.AddScoped<IPrivacySettingsTargetsGetter, SqlitePrivacySettingsTargetsGetter>(); // Убедись, что этот класс рефакторен
            
            // --- Остальные ---
            services.AddScoped<IUserRepository, SqliteUserRepository>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IUserGetter, SqliteUserGetter>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IContactGroupRepository, SqliteContactGroupRepository>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IOutboundDBGetter, SqliteOutboundDBGetter>();
            services.AddScoped<IInboundDBGetter, SqliteInboundDBGetter>();
            services.AddScoped<IInboxRepository, SqliteInboxRepository>(); // Убедись, что этот класс рефакторен

            return services;
        }

        /// <summary>
        /// Registers services and repositories for MySQL persistence.
        /// </summary>
        private static IServiceCollection AddMySqlPersistence(this IServiceCollection services, string connectionString)
        {
            // Configure FluentMigrator for MySQL
            //services.ConfigureRunner(rb => rb.AddMySql5());

            // Register a transient IDbConnection factory for MySQL
            services.AddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));

            // Register Unit of Work implementation for MySQL
            services.AddScoped<Database.UnitOfWork.IUnitOfWork, Database.UnitOfWork.MySqlUnitOfWork>();

            // ========================================================================
            // == РЕПОЗИТОРИИ (ПРЯМОЙ ДОСТУП К ДАННЫМ)
            // ========================================================================
            // "Глупые" классы, которые только выполняют SQL.
            services.AddScoped<IContactRepository, MySqlContactRepository>();
            services.AddScoped<IDefaultActionRepository, MySqlDefaultActionRepository>();
            services.AddScoped<IDefaultActionTargetsRepository, MySqlDefaultActionTargetsRepository>();
            services.AddScoped<IGroupRepository, MySqlGroupRepository>();
            services.AddScoped<IPrivacySettingsRepository, MySqlPrivacySettingsRepository>();
            services.AddScoped<IPrivacySettingsTargetsRepository, MySqlPrivacySettingsTargetsRepository>();
            // ... Добавь сюда другие новые репозитории по мере их создания ...

            // ========================================================================
            // == ФАСАДЫ И GETTER'ы (СТАРЫЕ ИНТЕРФЕЙСЫ)
            // ========================================================================
            // Эти классы реализуют старые интерфейсы, но под капотом вызывают новые UoW сервисы или репозитории.
            // Это обеспечивает обратную совместимость с кодом бота.
            
            // --- Контакты ---
            services.AddScoped<IContactAdder, MySqlContactAdder>();
            services.AddScoped<IContactRemover, MySqlContactRemover>();
            services.AddScoped<IContactSetter, MySqlContactSetter>();
            services.AddScoped<IContactGetter, MySqlContactGetter>(); // Убедись, что этот класс рефакторен
            
            // --- Группы ---
            services.AddScoped<IGroupRepository, MySqlGroupRepository>();
            services.AddScoped<IGroupSetter, MySqlGroupSetter>();
            services.AddScoped<IGroupGetter, MySqlGroupGetter>(); // Убедись, что этот класс рефакторен

            // --- Действия по умолчанию ---
            services.AddScoped<IDefaultAction, MySqlDefaultAction>();
            services.AddScoped<IDefaultActionSetter, MySqlDefaultActionSetter>();
            services.AddScoped<IDefaultActionGetter, MySqlDefaultActionGetter>(); // Убедись, что этот класс рефакторен

            // --- Настройки Приватности ---
            services.AddScoped<IPrivacySettingsSetter, MySqlPrivacySettingsSetter>();
            services.AddScoped<IPrivacySettingsGetter, MySqlPrivacySettingsGetter>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IPrivacySettingsTargetsSetter, MySqlPrivacySettingsTargetsSetter>();
            services.AddScoped<IPrivacySettingsTargetsGetter, MySqlPrivacySettingsTargetsGetter>(); // Убедись, что этот класс рефакторен
            
            // --- Остальные ---
            services.AddScoped<IUserRepository, MySqlUserRepository>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IUserGetter, MySqlUserGetter>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IContactGroupRepository, MySqlContactGroupRepository>(); // Убедись, что этот класс рефакторен
            services.AddScoped<IOutboundDBGetter, MySqlOutboundDBGetter>();
            services.AddScoped<IInboundDBGetter, MySqlInboundDBGetter>();
            services.AddScoped<IInboxRepository, MySqlInboxRepository>(); // Убедись, что этот класс рефакторен

            return services;
        }

        /// <summary>
        /// Applies any pending database migrations.
        /// </summary>
public static async Task ApplyDatabaseMigrationsAsync(this IHost host)
{
    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<IHost>>();
    var migrationRunner = services.GetRequiredService<IMigrationRunner>();
    
    // Получаем доступ к соединению и конфигурации для нашего "кода-спасателя"
    using var connection = services.GetRequiredService<IDbConnection>();
    var configuration = services.GetRequiredService<IConfiguration>();
    var databaseName = configuration["AppSettings:DatabaseName"];

    if (string.IsNullOrEmpty(databaseName))
    {
        logger.LogError("DatabaseName is not specified in AppSettings. Cannot perform consistency check.");
        throw new InvalidOperationException("DatabaseName is not configured.");
    }

    try
    {
        // --- НАЧАЛО КОДА-СПАСАТЕЛЯ ---
        logger.LogInformation("Performing database consistency check before migration...");

        // SQL-запросы для проверки существования таблиц в MySQL
        var checkTableSql = "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = @dbName AND table_name = @tableName";
        
        var versionInfoExists = await connection.QuerySingleAsync<int>(checkTableSql, new { dbName = databaseName, tableName = "VersionInfo" }) == 1;
        var usersTableExists = await connection.QuerySingleAsync<int>(checkTableSql, new { dbName = databaseName, tableName = "Users" }) == 1;


        Log.Debug($"VersionInfo table exists: {versionInfoExists}");
        Log.Debug($"Users table exists: {usersTableExists}");
                // Наша проблемная ситуация: таблицы данных есть, а таблицы миграций - нет.
                if (!versionInfoExists && usersTableExists)
                {
                    logger.LogWarning("!!! Inconsistent database state detected (Data tables exist, but VersionInfo is missing). Attempting to fix it manually. !!!");

                    // 1. Создаем таблицу VersionInfo вручную, как это сделал бы FluentMigrator
                    logger.LogInformation("Manually creating 'VersionInfo' table...");
                    await connection.ExecuteAsync(@"
                CREATE TABLE `VersionInfo` (
                    `Version` BIGINT NOT NULL,
                    `AppliedOn` DATETIME NULL,
                    `Description` NVARCHAR(1024) NULL,
                    PRIMARY KEY (`Version`)
                );");
                    logger.LogInformation("'VersionInfo' table created successfully.");

                    // 2. Вставляем записи о уже "примененных" миграциях.
                    //    УКАЖИТЕ ЗДЕСЬ ВЕРСИИ ВСЕХ ВАШИХ СУЩЕСТВУЮЩИХ МИГРАЦИЙ!
                    logger.LogInformation("Manually marking existing migrations as applied...");
                    await connection.ExecuteAsync(
                        "INSERT INTO VersionInfo (Version, AppliedOn, Description) VALUES (@Version, @AppliedOn, @Description)",
                        new[]
                        {
                    // Версия из вашего файла M001_CreateInitialSchema.cs
                    new { Version = 2025100901L, AppliedOn = DateTime.UtcNow, Description = "Manual Fix - M001_CreateInitialSchema" },
                    // Версия из вашего файла M002_CreateMySqlSpecificObjects.cs
                    new { Version = 2025100902L, AppliedOn = DateTime.UtcNow, Description = "Manual Fix - M002_CreateMySqlSpecificObjects" }
                        }
                    );
                    logger.LogInformation("Migrations marked as applied. The database state is now consistent.");
                }
                else
                {
                    logger.LogInformation("Database consistency check passed.");
                }
        // --- КОНЕЦ КОДА-СПАСАТЕЛЯ ---

        logger.LogWarning("--- STARTING MIGRATION ---");
        
        // Теперь запускаем мигратор. Он увидит, что все в порядке.
        migrationRunner.MigrateUp();
        
        logger.LogWarning("--- MIGRATION FINISHED ---");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during migration!");
        throw; // Пробрасываем исключение, чтобы приложение не запустилось с ошибкой.
    }
}        /// Extracts and formats the connection string from configuration.
        /// (This is the refactored version of your original method)
        /// </summary>
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
    }
}