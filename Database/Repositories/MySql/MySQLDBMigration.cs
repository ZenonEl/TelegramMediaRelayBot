// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Data;
using FluentMigrator;
namespace TelegramMediaRelayBot.Database;

// MySQL Implementation
[Migration(20240427)]
public class MySQLDBMigration : BaseDBMigration
{
    protected override string DBType => "mysql";

    public override void Down()
    {
        throw new NotImplementedException();
    }

    protected override void CreateSpecificConstraints()
    {
        if (Config.dbType != "mysql") return;

        // ========== Contacts ==========
        Create.PrimaryKey("PK_Contacts")
            .OnTable("Contacts").Columns("UserId", "ContactId");

        Create.ForeignKey("FK_Contacts_UserId")
            .FromTable("Contacts").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("ID");

        Create.ForeignKey("FK_Contacts_ContactId")
            .FromTable("Contacts").ForeignColumn("ContactId")
            .ToTable("Users").PrimaryColumn("ID");

        // ========== MutedContacts ==========
        Create.UniqueConstraint("UQ_MutedContacts")
            .OnTable("MutedContacts")
            .Columns("MutedByUserId", "MutedContactId");

        Create.ForeignKey("FK_MutedContacts_MutedByUserId")
            .FromTable("MutedContacts").ForeignColumn("MutedByUserId")
            .ToTable("Users").PrimaryColumn("ID");

        Create.ForeignKey("FK_MutedContacts_MutedContactId")
            .FromTable("MutedContacts").ForeignColumn("MutedContactId")
            .ToTable("Users").PrimaryColumn("ID");

        // ========== UsersGroups ==========
        Create.UniqueConstraint("UQ_UsersGroups")
            .OnTable("UsersGroups")
            .Columns("UserId", "GroupName");

        Create.ForeignKey("FK_UsersGroups_UserId")
            .FromTable("UsersGroups").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("ID");

        // ========== GroupMembers ==========
        Create.UniqueConstraint("UQ_GroupMembers")
            .OnTable("GroupMembers")
            .Columns("GroupId", "ContactId");

        Create.ForeignKey("FK_GroupMembers_UserId")
            .FromTable("GroupMembers").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("ID");

        Create.ForeignKey("FK_GroupMembers_GroupId")
            .FromTable("GroupMembers").ForeignColumn("GroupId")
            .ToTable("UsersGroups").PrimaryColumn("ID");

        // ========== DefaultUsersActions ==========
        Create.UniqueConstraint("UQ_DefaultUsersActions")
            .OnTable("DefaultUsersActions")
            .Columns("UserId", "Type");

        Create.ForeignKey("FK_DefaultUsersActions_UserId")
            .FromTable("DefaultUsersActions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("ID");

        // ========== DefaultUsersActionTargets ==========
        Create.UniqueConstraint("UQ_DefaultUsersActionTargets")
            .OnTable("DefaultUsersActionTargets")
            .Columns("UserId", "ActionID", "TargetID");

        Create.ForeignKey("FK_DefaultUsersActionTargets_UserId")
            .FromTable("DefaultUsersActionTargets").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_DefaultUsersActionTargets_ActionID")
            .FromTable("DefaultUsersActionTargets").ForeignColumn("ActionID")
            .ToTable("DefaultUsersActions").PrimaryColumn("ID").OnDelete(Rule.Cascade);

        // ========== PrivacySettings ==========
        Create.UniqueConstraint("UQ_PrivacySettings")
            .OnTable("PrivacySettings")
            .Columns("UserId", "Type");

        Create.ForeignKey("FK_PrivacySettings_UserId")
            .FromTable("PrivacySettings").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("ID");
    }
}
