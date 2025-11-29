// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.Infrastructure.Backup;

public sealed class SqliteBackupProvider : IBackupProvider
{
    private readonly IConfiguration _configuration;
    private readonly BackupConfiguration _settings;

    public SqliteBackupProvider(IConfiguration configuration, IOptions<BackupConfiguration> settings)
    {
        _configuration = configuration;
        _settings = settings.Value;
    }

    private string GetDbFilePath()
    {
        var cs = _configuration["AppSettings:SqlConnectionString"] ?? string.Empty;
        // Expect formats like: Data Source=/path/to/db.sqlite
        var prefix = "Data Source=";
        var start = cs.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            var path = cs[(start + prefix.Length)..].Trim();
            return path;
        }
        // fallback to default in AppContext.BaseDirectory
        return System.IO.Path.Combine(AppContext.BaseDirectory, "TelegramMediaRelayBot.db");
    }

    public async Task<BackupDescriptor> CreateAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_settings.Storage.Path);
        var dbPath = GetDbFilePath();
        var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var destFile = System.IO.Path.Combine(_settings.Storage.Path, $"{ts}_sqlite.db");

        // Prefer VACUUM INTO for consistent snapshot
        if (_settings.Sqlite.UseVacuumInto)
        {
            await using var conn = new SqliteConnection(_configuration["AppSettings:SqlConnectionString"]);
            await conn.OpenAsync(ct);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"VACUUM INTO '{destFile.Replace("'", "''")}'";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        else
        {
            // Fallback: copy file safely
            System.IO.File.Copy(dbPath, destFile, overwrite: true);
        }

        string finalPath = destFile;
        if (_settings.Storage.UseGzip)
        {
            var gz = destFile + ".gz";
            await using var input = System.IO.File.OpenRead(destFile);
            await using var output = System.IO.File.Create(gz);
            await using var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: false);
            await input.CopyToAsync(gzip, ct);
            System.IO.File.Delete(destFile);
            finalPath = gz;
        }

        string? checksum = await ComputeSha256Async(finalPath, ct);
        var info = new System.IO.FileInfo(finalPath);
        var descriptor = new BackupDescriptor(
            BackupId: System.IO.Path.GetFileNameWithoutExtension(finalPath),
            FilePath: finalPath,
            SizeBytes: info.Length,
            DbType: "sqlite",
            CreatedUtc: DateTime.UtcNow,
            ChecksumSha256: checksum);
        Log.Information("Backup created: {File} ({Size} bytes)", finalPath, info.Length);
        return descriptor;
    }

    public async Task RestoreAsync(string backupFilePath, CancellationToken ct)
    {
        string input = backupFilePath;
        // Decompress if .gz
        if (input.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            var temp = System.IO.Path.GetTempFileName();
            await using var src = System.IO.File.OpenRead(input);
            await using var gz = new System.IO.Compression.GZipStream(src, System.IO.Compression.CompressionMode.Decompress);
            await using var dst = System.IO.File.OpenWrite(temp);
            await gz.CopyToAsync(dst, ct);
            input = temp;
        }

        var targetDb = GetDbFilePath();
        var backupCopy = targetDb + ".restore";
        System.IO.File.Copy(input, backupCopy, overwrite: true);
        // Atomic-ish swap: move current to .old, then move restore into place
        var old = targetDb + ".old";
        if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
        if (System.IO.File.Exists(targetDb)) System.IO.File.Move(targetDb, old);
        System.IO.File.Move(backupCopy, targetDb);
        Log.Information("Restore completed to {Target}", targetDb);
    }

    public Task<IReadOnlyList<BackupDescriptor>> ListAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_settings.Storage.Path);
        var files = System.IO.Directory.EnumerateFiles(_settings.Storage.Path)
            .Where(f => f.EndsWith(".db") || f.EndsWith(".db.gz"))
            .Select(f => new BackupDescriptor(
                System.IO.Path.GetFileNameWithoutExtension(f),
                f,
                new System.IO.FileInfo(f).Length,
                "sqlite",
                System.IO.File.GetCreationTimeUtc(f)))
            .OrderByDescending(b => b.CreatedUtc)
            .Cast<BackupDescriptor>()
            .ToList();
        return Task.FromResult((IReadOnlyList<BackupDescriptor>)files);
    }

    public Task<bool> DeleteAsync(string backupFilePath, CancellationToken ct)
    {
        if (System.IO.File.Exists(backupFilePath))
        {
            System.IO.File.Delete(backupFilePath);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<Stream> GetBackupStreamAsync(string backupFilePath, CancellationToken ct)
    {
        Stream s = System.IO.File.OpenRead(backupFilePath);
        return Task.FromResult(s);
    }

    private static async Task<string> ComputeSha256Async(string file, CancellationToken ct)
    {
        await using var stream = System.IO.File.OpenRead(file);
        using var sha = SHA256.Create();
        byte[] hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }
}

