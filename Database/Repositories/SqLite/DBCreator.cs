// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Microsoft.Data.Sqlite;

namespace TelegramMediaRelayBot.Database.Repositories.Sqlite;

public class SqliteDBCreator
{
    public static void CreateDatabase(string connectionString)
    {
        if (!System.IO.File.Exists(GetDatabaseFilePath(connectionString)))
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
        }
    }

    private static string GetDatabaseFilePath(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        return builder.DataSource;
    }
}