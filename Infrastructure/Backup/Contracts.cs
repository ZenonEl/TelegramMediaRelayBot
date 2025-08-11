// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.Infrastructure.Backup;

public sealed record BackupDescriptor(
    string BackupId,
    string FilePath,
    long SizeBytes,
    string DbType,
    DateTime CreatedUtc,
    string? ChecksumSha256 = null
);

public interface IBackupProvider
{
    Task<BackupDescriptor> CreateAsync(CancellationToken ct);
    Task RestoreAsync(string backupFilePath, CancellationToken ct);
    Task<IReadOnlyList<BackupDescriptor>> ListAsync(CancellationToken ct);
    Task<bool> DeleteAsync(string backupFilePath, CancellationToken ct);
    Task<Stream> GetBackupStreamAsync(string backupFilePath, CancellationToken ct);
}

public interface IBackupProviderFactory
{
    IBackupProvider Create(string dbType);
}

public interface IBackupOrchestrator
{
    Task InitializeAsync(CancellationToken ct);
    Task RunOnStartAsync(CancellationToken ct);
    Task RunOnShutdownAsync(CancellationToken ct);
}

