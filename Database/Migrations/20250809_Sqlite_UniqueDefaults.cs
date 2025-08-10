// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using FluentMigrator;

namespace TelegramMediaRelayBot.Database;

[Migration(2025080901)]
[Tags("sqlite")]
public class SqliteUniqueDefaultsMigration : Migration
{
    public override void Up()
    {
        // Merge duplicate DefaultUsersActions rows per (UserId, Type)
        Execute.Sql(@"
UPDATE DefaultUsersActions AS d
SET Action = COALESCE(
        d.Action,
        (
            SELECT d2.Action FROM DefaultUsersActions d2
            WHERE d2.UserId = d.UserId AND d2.Type = d.Type AND d2.Action IS NOT NULL
            ORDER BY d2.rowid DESC LIMIT 1
        )
    ),
    ActionCondition = COALESCE(
        d.ActionCondition,
        (
            SELECT d2.ActionCondition FROM DefaultUsersActions d2
            WHERE d2.UserId = d.UserId AND d2.Type = d.Type AND d2.ActionCondition IS NOT NULL
            ORDER BY d2.rowid DESC LIMIT 1
        )
    )
WHERE d.rowid IN (
    SELECT MAX(rowid) FROM DefaultUsersActions GROUP BY UserId, Type
);

DELETE FROM DefaultUsersActions
WHERE rowid NOT IN (
    SELECT MAX(rowid) FROM DefaultUsersActions GROUP BY UserId, Type
);

-- Remove duplicates in DefaultUsersActionTargets as well
DELETE FROM DefaultUsersActionTargets
WHERE rowid NOT IN (
    SELECT MAX(rowid) FROM DefaultUsersActionTargets GROUP BY UserId, ActionID, TargetID
);

-- Create unique indexes to enforce upserts
CREATE UNIQUE INDEX IF NOT EXISTS UQ_DefaultUsersActions_User_Type ON DefaultUsersActions(UserId, Type);
CREATE UNIQUE INDEX IF NOT EXISTS UQ_DefaultUsersActionTargets_User_Action_Target ON DefaultUsersActionTargets(UserId, ActionID, TargetID);

-- Optional parity with MySQL (not strictly required for this bug)
CREATE UNIQUE INDEX IF NOT EXISTS UQ_PrivacySettings_User_Type ON PrivacySettings(UserId, Type);

");
    }

    public override void Down()
    {
        // No down migration for safety
    }
}

