// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.Infrastructure.Backup;

public sealed class MySqlBackupProvider : IBackupProvider
{
    private readonly IConfiguration _configuration;
    private readonly BackupConfiguration _settings;

    public MySqlBackupProvider(IConfiguration configuration, IOptions<BackupConfiguration> settings)
    {
        _configuration = configuration;
        _settings = settings.Value;
    }

    private string GetConnectionString() => _configuration["AppSettings:SqlConnectionString"] ?? string.Empty;

    public async Task<BackupDescriptor> CreateAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_settings.Storage.Path);
        var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var baseFile = System.IO.Path.Combine(_settings.Storage.Path, $"{ts}_mysql.sql");

        await RunDumpAsync(baseFile, ct);

        string finalPath = baseFile;
        if (_settings.Storage.UseGzip)
        {
            var gz = baseFile + ".gz";
            await using var input = System.IO.File.OpenRead(baseFile);
            await using var output = System.IO.File.Create(gz);
            await using var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: false);
            await input.CopyToAsync(gzip, ct);
            System.IO.File.Delete(baseFile);
            finalPath = gz;
        }

        string checksum = await ComputeSha256Async(finalPath, ct);
        var info = new System.IO.FileInfo(finalPath);
        var descriptor = new BackupDescriptor(System.IO.Path.GetFileNameWithoutExtension(finalPath), finalPath, info.Length, "mysql", DateTime.UtcNow, checksum);
        Log.Information("Backup created: {File} ({Size} bytes)", finalPath, info.Length);
        return descriptor;
    }

    public async Task RestoreAsync(string backupFilePath, CancellationToken ct)
    {
        string scriptPath = backupFilePath;
        if (scriptPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            var temp = System.IO.Path.GetTempFileName();
            await using var src = System.IO.File.OpenRead(scriptPath);
            await using var gz = new System.IO.Compression.GZipStream(src, System.IO.Compression.CompressionMode.Decompress);
            await using var dst = System.IO.File.OpenWrite(temp);
            await gz.CopyToAsync(dst, ct);
            scriptPath = temp;
        }

        await RunClientAsync(scriptPath, ct);
        Log.Information("Restore completed from {File}", backupFilePath);
    }

    public Task<IReadOnlyList<BackupDescriptor>> ListAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_settings.Storage.Path);
        var files = System.IO.Directory.EnumerateFiles(_settings.Storage.Path)
            .Where(f => f.EndsWith(".sql") || f.EndsWith(".sql.gz"))
            .Select(f => new BackupDescriptor(System.IO.Path.GetFileNameWithoutExtension(f), f, new System.IO.FileInfo(f).Length, "mysql", System.IO.File.GetCreationTimeUtc(f)))
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

    private async Task RunDumpAsync(string outputSqlPath, CancellationToken ct)
    {
        var cs = GetConnectionString();
        // Expect standard CS, user and password
        var args = $"{_settings.MySql.Cli.ExtraArgs} --result-file=\"{outputSqlPath}\" --routines --triggers --events --databases {ExtractDatabase(cs)} {BuildAuthArgs(cs)}";
        await RunProcessAsync(_settings.MySql.Cli.DumpPath, args, ct);
    }

    private async Task RunClientAsync(string scriptPath, CancellationToken ct)
    {
        var cs = GetConnectionString();
        var args = $"{_settings.MySql.Cli.ExtraArgs} {BuildAuthArgs(cs)} {ExtractDatabase(cs)} < \"{scriptPath}\"";
        // Use shell to support input redirection
        var shell = "/bin/bash";
        var shellArgs = $"-lc \"{_settings.MySql.Cli.ClientPath} {args}\"";
        await RunProcessAsync(shell, shellArgs, ct);
    }

    private static string ExtractDatabase(string cs)
    {
        // crude parse for Database=xxx;
        var key = "Database=";
        var idx = cs.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var rest = cs[(idx + key.Length)..];
            var end = rest.IndexOf(';');
            return end >= 0 ? rest[..end] : rest;
        }
        return string.Empty;
    }

    private static string BuildAuthArgs(string cs)
    {
        string user = ExtractValue(cs, "Uid=") ?? ExtractValue(cs, "User Id=") ?? "root";
        string pwd = ExtractValue(cs, "Pwd=") ?? ExtractValue(cs, "Password=") ?? string.Empty;
        return $"-u\"{user}\"{(string.IsNullOrEmpty(pwd) ? string.Empty : " -p\"" + pwd + "\"")}";
    }

    private static string? ExtractValue(string cs, string key)
    {
        var idx = cs.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var rest = cs[(idx + key.Length)..];
        var end = rest.IndexOf(';');
        return end >= 0 ? rest[..end] : rest;
    }

    private static async Task RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.Start();
        string stdOut = await proc.StandardOutput.ReadToEndAsync();
        string stdErr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync(ct);
        if (proc.ExitCode != 0)
        {
            Log.Error("MySQL CLI exited with {Code}. StdErr: {Err}", proc.ExitCode, stdErr);
            throw new InvalidOperationException($"MySQL CLI exit code {proc.ExitCode}");
        }
        if (!string.IsNullOrWhiteSpace(stdErr))
        {
            Log.Warning("MySQL CLI stderr: {Err}", stdErr.Trim());
        }
        if (!string.IsNullOrWhiteSpace(stdOut))
        {
            Log.Debug("MySQL CLI stdout: {Out}", stdOut.Trim());
        }
    }

    private static async Task<string> ComputeSha256Async(string file, CancellationToken ct)
    {
        await using var stream = System.IO.File.OpenRead(file);
        using var sha = SHA256.Create();
        byte[] hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }
}

