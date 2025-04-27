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
using Microsoft.Data.Sqlite;

namespace TelegramMediaRelayBot.Database;

// SQLite Implementation
[Migration(20240428, TransactionBehavior.None)]
public class SQLiteDBMigration : BaseDBMigration
{
    protected override string DBType => "sqlite";

    public override void Down()
    {
        throw new NotImplementedException();
    }

    protected override void CreateSpecificConstraints()
    {
        if (Config.dbType != "sqlite") return;
        var builder = new SqliteConnectionStringBuilder(Config.sqlConnectionString);
        if (System.IO.File.Exists(builder.DataSource)) return;

        // Включаем поддержку foreign keys
        Execute.Sql("PRAGMA foreign_keys = ON;");

        // ========== Индексы для Contacts ==========
        Create.Index("IX_Contacts_UserId")
            .OnTable("Contacts")
            .OnColumn("UserId");

        Create.Index("IX_Contacts_ContactId")
            .OnTable("Contacts")
            .OnColumn("ContactId");

        // ========== Индексы для MutedContacts ==========
        Create.Index("IX_MutedContacts_MutedByUserId")
            .OnTable("MutedContacts")
            .OnColumn("MutedByUserId");

        Create.Index("IX_MutedContacts_MutedContactId")
            .OnTable("MutedContacts")
            .OnColumn("MutedContactId");

        // ========== Индексы для UsersGroups ==========
        Create.Index("IX_UsersGroups_UserId")
            .OnTable("UsersGroups")
            .OnColumn("UserId");

        // ========== Индексы для GroupMembers ==========
        Create.Index("IX_GroupMembers_UserId")
            .OnTable("GroupMembers")
            .OnColumn("UserId");

        Create.Index("IX_GroupMembers_GroupId")
            .OnTable("GroupMembers")
            .OnColumn("GroupId");

        Create.Index("IX_GroupMembers_ContactId")
            .OnTable("GroupMembers")
            .OnColumn("ContactId");

        // ========== Индексы для DefaultUsersActions ==========
        Create.Index("IX_DefaultUsersActions_UserId")
            .OnTable("DefaultUsersActions")
            .OnColumn("UserId");

        // ========== Индексы для DefaultUsersActionTargets ==========
        Create.Index("IX_DefaultUsersActionTargets_UserId")
            .OnTable("DefaultUsersActionTargets")
            .OnColumn("UserId");

        Create.Index("IX_DefaultUsersActionTargets_ActionID")
            .OnTable("DefaultUsersActionTargets")
            .OnColumn("ActionID");

        // ========== Индексы для PrivacySettings ==========
        Create.Index("IX_PrivacySettings_UserId")
            .OnTable("PrivacySettings")
            .OnColumn("UserId");
    }
}
