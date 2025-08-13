// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using FluentMigrator;

namespace TelegramMediaRelayBot.Database;

[Migration(2025080902)]
[Tags("mysql")]
public class MySqlUniqueDefaultsMigration : Migration
{
	public override void Up()
	{
		// Remove duplicates in DefaultUsersActions, keep the latest by ID
		Execute.Sql(@"
	DELETE d1 FROM DefaultUsersActions d1
	JOIN DefaultUsersActions d2
	  ON d1.UserId = d2.UserId AND d1.Type = d2.Type AND d1.ID < d2.ID;
	");

		// Remove duplicates in DefaultUsersActionTargets, keep the latest by ID
		Execute.Sql(@"
	DELETE t1 FROM DefaultUsersActionTargets t1
	JOIN DefaultUsersActionTargets t2
	  ON t1.UserId = t2.UserId AND t1.ActionID = t2.ActionID AND t1.TargetID = t2.TargetID AND t1.ID < t2.ID;
	");

		// Remove duplicates in PrivacySettings, keep the latest by ID
		Execute.Sql(@"
	DELETE p1 FROM PrivacySettings p1
	JOIN PrivacySettings p2
	  ON p1.UserId = p2.UserId AND p1.Type = p2.Type AND p1.ID < p2.ID;
	");

        // Ensure unique constraints exist (idempotent) — через FluentMigrator DSL
        if (!Schema.Table("DefaultUsersActions").Index("UQ_DefaultUsersActions").Exists())
        {
            Create.Index("UQ_DefaultUsersActions")
                .OnTable("DefaultUsersActions")
                .OnColumn("UserId").Ascending()
                .OnColumn("Type").Ascending()
                .WithOptions().Unique();
        }

        if (!Schema.Table("DefaultUsersActionTargets").Index("UQ_DefaultUsersActionTargets").Exists())
        {
            Create.Index("UQ_DefaultUsersActionTargets")
                .OnTable("DefaultUsersActionTargets")
                .OnColumn("UserId").Ascending()
                .OnColumn("ActionID").Ascending()
                .OnColumn("TargetID").Ascending()
                .WithOptions().Unique();
        }

        if (!Schema.Table("PrivacySettings").Index("UQ_PrivacySettings").Exists())
        {
            Create.Index("UQ_PrivacySettings")
                .OnTable("PrivacySettings")
                .OnColumn("UserId").Ascending()
                .OnColumn("Type").Ascending()
                .WithOptions().Unique();
        }
	}

	public override void Down()
	{
		// No down migration to avoid data loss
	}
}

