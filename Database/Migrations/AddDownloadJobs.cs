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

namespace TelegramMediaRelayBot.Database.Migrations;

[Migration(20260717)]
public class AddDownloadJobs : Migration
{
    public override void Up()
    {
        if (!Schema.Table("DownloadJobs").Exists())
        {
            Execute.Sql(@"
                CREATE TABLE DownloadJobs (
                    Id TEXT PRIMARY KEY,
                    ChatId INTEGER NOT NULL,
                    Url TEXT NOT NULL,
                    Caption TEXT NOT NULL DEFAULT '',
                    TargetUserIdsJson TEXT NULL,
                    IsGroupChat INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                )");
        }
    }

    public override void Down()
    {
        Delete.Table("DownloadJobs");
    }
}
