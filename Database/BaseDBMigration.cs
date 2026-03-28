// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using FluentMigrator;


namespace TelegramMediaRelayBot.Database;

public abstract class BaseDBMigration : Migration
{
    protected abstract string DBType { get; }

    public override void Up()
    {
        CreateCommonTables();
        CreateSpecificConstraints();
    }

    private void CreateCommonTables()
    {
        // Users
        if (!Schema.Table("Users").Exists())
        {
            Create.Table("Users")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("TelegramID").AsInt64().NotNullable()
                .WithColumn("Name").AsString(255).NotNullable()
                .WithColumn("Link").AsString(255).NotNullable()
                .WithColumn("Status").AsString(255).Nullable();
        }

        // Contacts
        if (!Schema.Table("Contacts").Exists())
        {
            Execute.Sql(@"
                CREATE TABLE Contacts (
                    UserId INTEGER NOT NULL,
                    ContactId INTEGER NOT NULL,
                    status TEXT,
                    MutedUntil TEXT,
                    PRIMARY KEY (UserId, ContactId)
                )");
        }

        // UsersGroups
        if (!Schema.Table("UsersGroups").Exists())
        {
            Create.Table("UsersGroups")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("GroupName").AsString(255).NotNullable()
                .WithColumn("Description").AsString(int.MaxValue).Nullable()
                .WithColumn("IsDefaultEnabled").AsBoolean().NotNullable().WithDefaultValue(true);
        }

        // GroupMembers
        if (!Schema.Table("GroupMembers").Exists())
        {
            Create.Table("GroupMembers")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("ContactId").AsInt32().NotNullable()
                .WithColumn("GroupId").AsInt32().NotNullable()
                .WithColumn("Status").AsBoolean().NotNullable().WithDefaultValue(true);
        }

        // DefaultUsersActions
        if (!Schema.Table("DefaultUsersActions").Exists())
        {
            Create.Table("DefaultUsersActions")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("Type").AsString(255).NotNullable()
                .WithColumn("Action").AsString(255).Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("ActionCondition").AsString(255).Nullable();
        }

        // DefaultUsersActionTargets
        if (!Schema.Table("DefaultUsersActionTargets").Exists())
        {
            Create.Table("DefaultUsersActionTargets")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("ActionID").AsInt32().NotNullable()
                .WithColumn("TargetType").AsString(255).NotNullable()
                .WithColumn("TargetID").AsString(255).NotNullable();
        }

        // PrivacySettings
        if (!Schema.Table("PrivacySettings").Exists())
        {
            Create.Table("PrivacySettings")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("Type").AsString(255).NotNullable()
                .WithColumn("Action").AsString(255).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("ActionCondition").AsString(255).Nullable();
        }

        // PrivacySettingsTargets
        if (!Schema.Table("PrivacySettingsTargets").Exists())
        {
            Create.Table("PrivacySettingsTargets")
                .WithColumn("ID").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("PrivacySettingId").AsInt32().NotNullable()
                .WithColumn("TargetType").AsString(255).NotNullable()
                .WithColumn("TargetValue").AsString(255).NotNullable();
        }

        // UserStates
        if (!Schema.Table("UserStates").Exists())
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS UserStates (
                    ChatId INTEGER PRIMARY KEY,
                    StateName TEXT NOT NULL,
                    StateDataJson TEXT NOT NULL DEFAULT '{}',
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    ExpiresAt TEXT NOT NULL
                )");

            Execute.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_UserStates_ExpiresAt ON UserStates(ExpiresAt)");
        }
    }

    protected abstract void CreateSpecificConstraints();
}
