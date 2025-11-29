// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Config;

public sealed class BackupConfiguration
{
    public bool Enabled { get; set; } = false;
    public BackupScheduleConfiguration Schedule { get; set; } = new();
    public BackupStorageConfiguration Storage { get; set; } = new();
    public BackupEncryptionConfiguration Encryption { get; set; } = new();
    public string? ProviderOverride { get; set; } = null;
    public MySqlCliConfiguration MySql { get; set; } = new();
    public SqliteConfiguration Sqlite { get; set; } = new();
    public BackupRestoreConfiguration Restore { get; set; } = new();
}

public sealed class BackupScheduleConfiguration
{
    public bool OnStart { get; set; } = false;
    public bool OnShutdown { get; set; } = false;
    public List<string> DailyTimes { get; set; } = new() { "03:00" };
    public string TimeZone { get; set; } = "UTC";
    public int InitialDelayMinutes { get; set; } = 5;
}

public sealed class BackupStorageConfiguration
{
    public string Path { get; set; } = "backups";
    public bool UseGzip { get; set; } = true;
    public BackupRetentionConfiguration Retention { get; set; } = new();
}

public sealed class BackupRetentionConfiguration
{
    public int MaxCount { get; set; } = 14;
    public int MaxAgeDays { get; set; } = 30;
}

public sealed class BackupEncryptionConfiguration
{
    public bool Enabled { get; set; } = false;
    public string PasswordEnvVar { get; set; } = "DB_BACKUP_PASSWORD";
}

public sealed class MySqlCliConfiguration
{
    public MySqlCliPathsConfiguration Cli { get; set; } = new();
}

public sealed class MySqlCliPathsConfiguration
{
    public string DumpPath { get; set; } = "mysqldump";
    public string ClientPath { get; set; } = "mysql";
    public string ExtraArgs { get; set; } = string.Empty;
}

public sealed class SqliteConfiguration
{
    public bool UseVacuumInto { get; set; } = true;
}

public sealed class BackupRestoreConfiguration
{
    public bool Enabled { get; set; } = false;
    public string BackupFile { get; set; } = string.Empty;
    public string RequireConfirmationFile { get; set; } = "restore.ok";
    public bool VerifyChecksum { get; set; } = true;
    public int TimeoutMinutes { get; set; } = 5;
    public string Mode { get; set; } = "Auto"; // For future extension
}

