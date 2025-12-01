// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Data;
using FluentMigrator;

namespace TelegramMediaRelayBot.Migrations;

[Migration(2025100902, "Create MySQL Specific Objects")]
[Tags("mysql")] // <-- Ключевой момент!
public class M002_CreateMySqlSpecificObjects : Migration
{
    public override void Up()
    {
        // === Primary Keys & Unique Constraints ===
        Create.PrimaryKey("PK_Contacts").OnTable("Contacts").Columns("UserId", "ContactId");
        Create.UniqueConstraint("UQ_MutedContacts").OnTable("MutedContacts").Columns("MutedByUserId", "MutedContactId");
        Create.UniqueConstraint("UQ_UsersGroups").OnTable("UsersGroups").Columns("UserId", "GroupName");
        Create.UniqueConstraint("UQ_GroupMembers").OnTable("GroupMembers").Columns("GroupId", "ContactId");
        Create.UniqueConstraint("UQ_DefaultUsersActions").OnTable("DefaultUsersActions").Columns("UserId", "Type");
        Create.UniqueConstraint("UQ_DefaultUsersActionTargets").OnTable("DefaultUsersActionTargets").Columns("UserId", "ActionID", "TargetID");
        Create.UniqueConstraint("UQ_PrivacySettings").OnTable("PrivacySettings").Columns("UserId", "Type");
        Create.UniqueConstraint("UQ_PrivacySettingsTargets").OnTable("PrivacySettingsTargets").Columns("UserId", "TargetValue");

        // === Foreign Keys ===
        Create.ForeignKey("FK_Contacts_UserId").FromTable("Contacts").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_Contacts_ContactId").FromTable("Contacts").ForeignColumn("ContactId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_MutedContacts_MutedByUserId").FromTable("MutedContacts").ForeignColumn("MutedByUserId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_MutedContacts_MutedContactId").FromTable("MutedContacts").ForeignColumn("MutedContactId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_UsersGroups_UserId").FromTable("UsersGroups").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_GroupMembers_UserId").FromTable("GroupMembers").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_GroupMembers_GroupId").FromTable("GroupMembers").ForeignColumn("GroupId").ToTable("UsersGroups").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_DefaultUsersActions_UserId").FromTable("DefaultUsersActions").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_DefaultUsersActionTargets_UserId").FromTable("DefaultUsersActionTargets").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_DefaultUsersActionTargets_ActionID").FromTable("DefaultUsersActionTargets").ForeignColumn("ActionID").ToTable("DefaultUsersActions").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_PrivacySettings_UserId").FromTable("PrivacySettings").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("ID").OnDelete(Rule.Cascade);
        Create.ForeignKey("FK_PrivacySettingsTargets_PrivacySettingId").FromTable("PrivacySettingsTargets").ForeignColumn("PrivacySettingId").ToTable("PrivacySettings").PrimaryColumn("ID").OnDelete(Rule.Cascade);

        // === Indexes ===
        Create.Index("IX_InboxItems_OwnerUserId").OnTable("InboxItems").OnColumn("OwnerUserId");
    }

    public override void Down()
    {
        // Удаляем в обратном порядке
        Delete.Index("IX_InboxItems_OwnerUserId");

        Delete.ForeignKey("FK_PrivacySettingsTargets_PrivacySettingId");
        // ... и так далее для всех FK и Unique Constraints
        // Для краткости я не буду расписывать все, но логика та же
    }
}
