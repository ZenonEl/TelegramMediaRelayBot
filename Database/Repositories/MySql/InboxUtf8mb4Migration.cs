// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using FluentMigrator;

namespace TelegramMediaRelayBot.Database;

[Migration(20241441)]
[Tags("mysql")]
public class MySQLInboxUtf8mb4Migration : Migration
{
    public override void Up()
    {
        // Ensure InboxItems can store 4-byte emojis in caption/payload/status
        if (Schema.Table("InboxItems").Exists())
        {
            Execute.Sql(@"ALTER TABLE InboxItems CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
            Execute.Sql(@"ALTER TABLE InboxItems 
                          MODIFY Caption LONGTEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
                          MODIFY PayloadJson LONGTEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                          MODIFY Status VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL;");
        }
    }

    public override void Down()
    {
        // No rollback needed
    }
}

