// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using FluentMigrator;

namespace TelegramMediaRelayBot.Database;

[Migration(20241440)]
[Tags("mysql")]
public class MySQLAddInboxMigration : Migration
{
    public override void Up()
    {
        if (!Schema.Table("InboxItems").Exists())
        {
            Create.Table("InboxItems")
                .WithColumn("ID").AsInt64().PrimaryKey().Identity()
                .WithColumn("OwnerUserId").AsInt32().NotNullable()
                .WithColumn("FromContactId").AsInt32().NotNullable()
                .WithColumn("Caption").AsString(int.MaxValue).Nullable()
                .WithColumn("PayloadJson").AsString(int.MaxValue).NotNullable()
                .WithColumn("Status").AsString(50).NotNullable().WithDefaultValue("new")
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
            Create.Index("IX_InboxItems_OwnerUserId")
                .OnTable("InboxItems")
                .OnColumn("OwnerUserId");
        }
    }

    public override void Down()
    {
        // No-op: keep data
    }
}

