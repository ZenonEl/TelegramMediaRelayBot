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
using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database.Repositories.MySql;
using TelegramMediaRelayBot.Database.Repositories.Sqlite;
using TelegramMediaRelayBot.TelegramBot;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;


namespace TelegramMediaRelayBot.Database;


public class FluentDBMigrator
{
    public static ServiceProvider GetCurrentServiceProvider(string DBType)
    {
        var serviceCollection = new ServiceCollection()
            .AddFluentMigratorCore()
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        switch (DBType.ToLower())
        {
            case "mysql":
                serviceCollection.ConfigureRunner(rb => rb
                    .AddMySql5()
                    .WithGlobalConnectionString(Config.sqlConnectionString)
                    .ScanIn(typeof(MySQLDBMigration).Assembly).For.Migrations());
                break;
                
            default:
                serviceCollection.ConfigureRunner(rb => rb
                    .AddSQLite()
                    .WithGlobalConnectionString(Config.sqlConnectionString)
                    .ScanIn(typeof(SQLiteDBMigration).Assembly).For.Migrations());
                break;
        }

        return serviceCollection.BuildServiceProvider(false);
    }

    public static WebApplicationBuilder CreateBuilderByDBType(string[] args, string DBType)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<ITelegramBotClient>(_ =>
            new TelegramBotClient(Config.telegramBotToken!));

        builder.Services.AddSingleton<CallbackQueryHandlersFactory>();
        builder.Services.AddSingleton<TGBot>();
        builder.Services.AddSingleton<Scheduler>();

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<IBotCallbackQueryHandlers>()
            .AddClasses(classes => classes.AssignableTo<IBotCallbackQueryHandlers>())
            .AsImplementedInterfaces()
            .WithTransientLifetime()
            );

        switch (DBType.ToLower())
        {
            case "mysql":
                MySqlDBCreator.CreateDatabase(Config.sqlConnectionString);
                builder.Services.AddSingleton<IUserRepository>(_ =>
                    new MySqlUserRepository(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IUserGetter>(_ =>
                    new MySqlUserGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IContactGroupRepository>(_ =>
                    new MySqlContactGroupRepository(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IContactAdder>(_ =>
                    new MySqlContactAdder(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IContactGetter>(_ =>
                    new MySqlContactGetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IContactSetter>(_ =>
                    new MySqlContactSetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IContactRemover>(_ =>
                    new MySqlContactRemover(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IOutboundDBGetter>(_ =>
                    new MySqlOutboundDBGetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IInboundDBGetter>(_ =>
                    new MySqlInboundDBGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IPrivacySettingsSetter>(_ =>
                    new MySqlPrivacySettingsSetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IPrivacySettingsGetter>(_ =>
                    new MySqlPrivacySettingsGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IDefaultAction>(_ =>
                    new MySqlDefaultAction(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IDefaultActionSetter>(_ =>
                    new MySqlDefaultActionSetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IDefaultActionGetter>(_ =>
                    new MySqlDefaultActionGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IGroupGetter>(_ =>
                    new MySqlGroupGetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IGroupSetter>(_ =>
                    new MySqlGroupSetter(Config.sqlConnectionString!));

                return builder;
            default:
                builder.Services.AddSingleton<IUserRepository>(_ =>
                    new SqliteUserRepository(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IUserGetter>(_ =>
                    new SqliteUserGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IContactGroupRepository>(_ =>
                    new SqliteContactGroupRepository(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IContactAdder>(_ =>
                    new SqliteContactAdder(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IContactGetter>(_ =>
                    new SqliteContactGetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IContactSetter>(_ =>
                    new SqliteContactSetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IContactRemover>(_ =>
                    new SqliteContactRemover(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IOutboundDBGetter>(_ =>
                    new SqliteOutboundDBGetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IInboundDBGetter>(_ =>
                    new SqliteInboundDBGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IPrivacySettingsSetter>(_ =>
                    new SqlitePrivacySettingsSetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IPrivacySettingsGetter>(_ =>
                    new SqlitePrivacySettingsGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IDefaultAction>(_ =>
                    new SqliteDefaultAction(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IDefaultActionSetter>(_ =>
                    new SqliteDefaultActionSetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IDefaultActionGetter>(_ =>
                    new SqliteDefaultActionGetter(Config.sqlConnectionString!));

                builder.Services.AddSingleton<IGroupGetter>(_ =>
                    new SqliteGroupGetter(Config.sqlConnectionString!));
                builder.Services.AddSingleton<IGroupSetter>(_ =>
                    new SqliteGroupSetter(Config.sqlConnectionString!));
                return builder;
        }
    }
}