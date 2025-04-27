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

// Deprecated code for FluentMigrator
// [Migration(20240427)]
// public class FluentDBMigrator : Migration
// {
//     public override void Up()
//     {

//         if (!Schema.Table("Users").Exists())
//         {
//             Create.Table("Users")
//                     .WithColumn("ID").AsInt32().PrimaryKey().Identity()
//                     .WithColumn("TelegramID").AsInt64().NotNullable()
//                     .WithColumn("Name").AsString(255).NotNullable()
//                     .WithColumn("Link").AsString(255).NotNullable()
//                     .WithColumn("Status").AsString(255).Nullable();
//         }

//         if (!Schema.Table("Contacts").Exists())
//         {
//             Create.Table("Contacts")
//                 .WithColumn("UserId").AsInt32().NotNullable()
//                 .WithColumn("ContactId").AsInt32().NotNullable()
//                 .WithColumn("status").AsString(255).Nullable();

//             Create.PrimaryKey("PK_Contacts").OnTable("Contacts").Columns("UserId", "ContactId");

//             Create.ForeignKey("FK_Contacts_UserId")
//                 .FromTable("Contacts").ForeignColumn("UserId")
//                 .ToTable("Users").PrimaryColumn("ID");

//             Create.ForeignKey("FK_Contacts_ContactId")
//                 .FromTable("Contacts").ForeignColumn("ContactId")
//                 .ToTable("Users").PrimaryColumn("ID");
//         }

//         if (!Schema.Table("MutedContacts").Exists())
//         {
//             Create.Table("MutedContacts")
//                 .WithColumn("MutedId").AsInt32().PrimaryKey().Identity()
//                 .WithColumn("MutedByUserId").AsInt32().NotNullable()
//                 .WithColumn("MutedContactId").AsInt32().NotNullable()
//                 .WithColumn("MuteDate").AsDateTime().NotNullable()
//                 .WithColumn("ExpirationDate").AsDateTime().Nullable()
//                 .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

//             Create.UniqueConstraint("UQ_MutedContacts_MutedByUserId_MutedContactId")
//                 .OnTable("MutedContacts")
//                 .Columns("MutedByUserId", "MutedContactId");

//             Create.ForeignKey("FK_MutedContacts_MutedByUserId")
//                 .FromTable("MutedContacts").ForeignColumn("MutedByUserId")
//                 .ToTable("Users").PrimaryColumn("ID");

//             Create.ForeignKey("FK_MutedContacts_MutedContactId")
//                 .FromTable("MutedContacts").ForeignColumn("MutedContactId")
//                 .ToTable("Users").PrimaryColumn("ID");
//         }

//         if (!Schema.Table("UsersGroups").Exists())
//         {
//             Create.Table("UsersGroups")
//                 .WithColumn("ID").AsInt32().PrimaryKey().Identity()
//                 .WithColumn("UserId").AsInt32().NotNullable()
//                 .WithColumn("GroupName").AsString(255).NotNullable()
//                 .WithColumn("Description").AsString(int.MaxValue).Nullable()
//                 .WithColumn("IsDefaultEnabled").AsBoolean().NotNullable().WithDefaultValue(true);

//             Create.UniqueConstraint("UQ_UsersGroups_UserId_GroupName")
//                 .OnTable("UsersGroups")
//                 .Columns("UserId", "GroupName");

//             Create.ForeignKey("FK_UsersGroups_UserId")
//                 .FromTable("UsersGroups").ForeignColumn("UserId")
//                 .ToTable("Users").PrimaryColumn("ID");
//         }

//         if (!Schema.Table("GroupMembers").Exists())
//         {
//             Create.Table("GroupMembers")
//                 .WithColumn("ID").AsInt32().PrimaryKey().Identity()
//                 .WithColumn("UserId").AsInt32().NotNullable()
//                 .WithColumn("ContactId").AsInt32().NotNullable()
//                 .WithColumn("GroupId").AsInt32().NotNullable()
//                 .WithColumn("Status").AsBoolean().NotNullable().WithDefaultValue(true);

//             Create.UniqueConstraint("UQ_GroupMembers_GroupId_ContactId")
//                 .OnTable("GroupMembers")
//                 .Columns("GroupId", "ContactId");

//             Create.ForeignKey("FK_GroupMembers_UserId")
//                 .FromTable("GroupMembers").ForeignColumn("UserId")
//                 .ToTable("Users").PrimaryColumn("ID");
//         }

//         if (!Schema.Table("DefaultUsersActions").Exists())
//         {
//             Create.Table("DefaultUsersActions")
//                 .WithColumn("ID").AsInt32().PrimaryKey().Identity()
//                 .WithColumn("UserId").AsInt32().NotNullable()
//                 .WithColumn("Type").AsString(255).NotNullable()
//                 .WithColumn("Action").AsString(255).Nullable()
//                 .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
//                 .WithColumn("ActionCondition").AsString(255).Nullable();

//             Create.UniqueConstraint("UQ_DefaultUsersActions_UserId_Type")
//                 .OnTable("DefaultUsersActions")
//                 .Columns("UserId", "Type");

//             Create.ForeignKey("FK_DefaultUsersActions_UserId")
//                 .FromTable("DefaultUsersActions").ForeignColumn("UserId")
//                 .ToTable("Users").PrimaryColumn("ID");
//         }

//         if (!Schema.Table("DefaultUsersActionTargets").Exists())
//         {
//             Create.Table("DefaultUsersActionTargets")
//                 .WithColumn("ID").AsInt32().PrimaryKey().Identity()
//                 .WithColumn("UserId").AsInt32().NotNullable()
//                 .WithColumn("ActionID").AsInt32().NotNullable()
//                 .WithColumn("TargetType").AsString(255).NotNullable()
//                 .WithColumn("TargetID").AsString(255).NotNullable();

//             Create.UniqueConstraint("UQ_DefaultUsersActionTargets_UserId_ActionID_TargetID")
//                 .OnTable("DefaultUsersActionTargets")
//                 .Columns("UserId", "ActionID", "TargetID");

//             Create.ForeignKey("FK_DefaultUsersActionTargets_UserId")
//                 .FromTable("DefaultUsersActionTargets").ForeignColumn("UserId")
//                 .ToTable("Users").PrimaryColumn("ID").OnDelete(System.Data.Rule.Cascade);

//             Create.ForeignKey("FK_DefaultUsersActionTargets_ActionID")
//                 .FromTable("DefaultUsersActionTargets").ForeignColumn("ActionID")
//                 .ToTable("DefaultUsersActions").PrimaryColumn("ID").OnDelete(System.Data.Rule.Cascade);
//         }

//         if (!Schema.Table("PrivacySettings").Exists())
//         {
//             Create.Table("PrivacySettings")
//                 .WithColumn("ID").AsInt32().PrimaryKey().Identity()
//                 .WithColumn("UserId").AsInt32().NotNullable()
//                 .WithColumn("Type").AsString(255).NotNullable()
//                 .WithColumn("Action").AsString(255).NotNullable()
//                 .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
//                 .WithColumn("ActionCondition").AsString(255).Nullable();

//             Create.UniqueConstraint("UQ_PrivacySettings_UserId_Type")
//                 .OnTable("PrivacySettings")
//                 .Columns("UserId", "Type");

//             Create.ForeignKey("FK_PrivacySettings_UserId")
//                 .FromTable("PrivacySettings").ForeignColumn("UserId")
//                 .ToTable("Users").PrimaryColumn("ID");
//         }
//     }

//     public override void Down()
//     {
//         Delete.Table("GroupMembers");
//         Delete.Table("UsersGroups");
//         Delete.Table("MutedContacts");
//         Delete.Table("Contacts");
//         Delete.Table("Users");
//     }
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