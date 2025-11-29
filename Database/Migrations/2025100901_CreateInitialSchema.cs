// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using FluentMigrator;

namespace TelegramMediaRelayBot.Migrations
{
    [Migration(2025100901, "Create Initial Schema")]
    public class M001_CreateInitialSchema : Migration
    {
        public override void Up()
        {
            // Users
            Create.Table("Users")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("TelegramID").AsInt64().NotNullable()
                .WithColumn("Name").AsString(255).NotNullable()
                .WithColumn("Link").AsString(255).NotNullable()
                .WithColumn("Status").AsString(255).Nullable();

            // Contacts - ОБРАТИ ВНИМАНИЕ: мы создаем ее универсально.
            // Составной первичный ключ будет добавлен в специфичных миграциях.
            Create.Table("Contacts")
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("ContactId").AsInt32().NotNullable()
                .WithColumn("status").AsString(255).Nullable();

            // MutedContacts
            Create.Table("MutedContacts")
                .WithColumn("MutedId").AsInt32().PrimaryKey().Identity()
                .WithColumn("MutedByUserId").AsInt32().NotNullable()
                .WithColumn("MutedContactId").AsInt32().NotNullable()
                .WithColumn("MuteDate").AsDateTime().NotNullable()
                .WithColumn("ExpirationDate").AsDateTime().Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

            // UsersGroups
            Create.Table("UsersGroups")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("GroupName").AsString(255).NotNullable()
                .WithColumn("Description").AsString(int.MaxValue).Nullable()
                .WithColumn("IsDefaultEnabled").AsBoolean().NotNullable().WithDefaultValue(true);

            // GroupMembers
            Create.Table("GroupMembers")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("ContactId").AsInt32().NotNullable()
                .WithColumn("GroupId").AsInt32().NotNullable()
                .WithColumn("Status").AsBoolean().NotNullable().WithDefaultValue(true);

            // DefaultUsersActions
            Create.Table("DefaultUsersActions")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("Type").AsString(255).NotNullable()
                .WithColumn("Action").AsString(255).Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("ActionCondition").AsString(255).Nullable();

            // DefaultUsersActionTargets
            Create.Table("DefaultUsersActionTargets")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("ActionID").AsInt32().NotNullable()
                .WithColumn("TargetType").AsString(255).NotNullable()
                .WithColumn("TargetID").AsString(255).NotNullable();

            // PrivacySettings
            Create.Table("PrivacySettings")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("Type").AsString(255).NotNullable()
                .WithColumn("Action").AsString(255).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("ActionCondition").AsString(255).Nullable();

            // PrivacySettingsTargets
            Create.Table("PrivacySettingsTargets")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("PrivacySettingId").AsInt32().NotNullable()
                .WithColumn("TargetType").AsString(255).NotNullable()
                .WithColumn("TargetValue").AsString(255).NotNullable();

            // InboxItems
            Create.Table("InboxItems")
                .WithColumn("ID").AsInt64().PrimaryKey().Identity()
                .WithColumn("OwnerUserId").AsInt32().NotNullable()
                .WithColumn("FromContactId").AsInt32().NotNullable()
                .WithColumn("Caption").AsString(int.MaxValue).Nullable()
                .WithColumn("PayloadJson").AsString(int.MaxValue).NotNullable()
                .WithColumn("Status").AsString(50).NotNullable().WithDefaultValue("new")
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
        }

        public override void Down()
        {
            // Удаляем в обратном порядке создания
            Delete.Table("InboxItems");
            Delete.Table("PrivacySettingsTargets");
            Delete.Table("PrivacySettings");
            Delete.Table("DefaultUsersActionTargets");
            Delete.Table("DefaultUsersActions");
            Delete.Table("GroupMembers");
            Delete.Table("UsersGroups");
            Delete.Table("MutedContacts");
            Delete.Table("Contacts");
            Delete.Table("Users");
        }
    }
}