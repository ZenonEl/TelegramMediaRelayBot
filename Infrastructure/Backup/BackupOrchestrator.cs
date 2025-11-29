// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.Infrastructure.Backup;

public sealed class BackupOrchestrator : IBackupOrchestrator
{
    private readonly IBackupProviderFactory _factory;
    private readonly IOptionsMonitor<BackupConfiguration> _settings;
    private readonly IHostApplicationLifetime _lifetime;
    private Timer? _dailyTimer;
    private string _dbType;

    public BackupOrchestrator(IBackupProviderFactory factory, IOptionsMonitor<BackupConfiguration> settings, IHostApplicationLifetime lifetime, IConfiguration configuration)
    {
        _factory = factory;
        _settings = settings;
        _lifetime = lifetime;
        _dbType = configuration["AppSettings:DatabaseType"] ?? "sqlite";
        _settings.OnChange(_ => ReconfigureTimers());
        _lifetime.ApplicationStopping.Register(() =>
        {
            try { _dailyTimer?.Change(Timeout.Infinite, Timeout.Infinite); _dailyTimer?.Dispose(); } catch { }
        });
    }

    public Task InitializeAsync(CancellationToken ct)
    {
        ReconfigureTimers();
        return Task.CompletedTask;
    }

    public async Task RunOnStartAsync(CancellationToken ct)
    {
        var cfg = _settings.CurrentValue;
        if (!cfg.Enabled) return;
        if (cfg.Restore.Enabled && System.IO.File.Exists(cfg.Restore.RequireConfirmationFile))
        {
            try
            {
                Log.Information("Restore requested. Starting restore from {File}", cfg.Restore.BackupFile);
                var provider = _factory.Create(_dbType);
                await provider.RestoreAsync(cfg.Restore.BackupFile, ct);
                if (cfg.Restore.VerifyChecksum)
                {
                    Log.Information("Checksum verification is marked as done externally for {File}", cfg.Restore.BackupFile);
                }
                // turn off restore flag to avoid loops
                Log.Information("Restore finished successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Restore failed");
            }
        }

        if (cfg.Schedule.OnStart)
        {
            await RunBackupAsync(ct, reason: "OnStart");
        }
    }

    public async Task RunOnShutdownAsync(CancellationToken ct)
    {
        var cfg = _settings.CurrentValue;
        if (!cfg.Enabled || !cfg.Schedule.OnShutdown) return;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(cfg.Restore.TimeoutMinutes));
        await RunBackupAsync(cts.Token, reason: "OnShutdown");
    }

    private async Task RunBackupAsync(CancellationToken ct, string reason)
    {
        try
        {
            var provider = _factory.Create(_dbType);
            Log.Information("Backup started ({Reason})", reason);
            var desc = await provider.CreateAsync(ct);
            await ApplyRetentionAsync(ct);
            Log.Information("Backup finished: {File} ({Size} bytes)", desc.FilePath, desc.SizeBytes);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Backup canceled ({Reason})", reason);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Backup failed ({Reason})", reason);
        }
    }

    private void ReconfigureTimers()
    {
        var cfg = _settings.CurrentValue;
        try
        {
            _dailyTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _dailyTimer?.Dispose();
            _dailyTimer = null;
            if (!cfg.Enabled) return;
            if (cfg.Schedule.DailyTimes is { Count: > 0 })
            {
                ScheduleNextDaily();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to reconfigure backup timers");
        }
    }

    private void ScheduleNextDaily()
    {
        var cfg = _settings.CurrentValue;
        var tz = TimeZoneInfo.FindSystemTimeZoneById(cfg.Schedule.TimeZone);
        var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
        DateTime? next = null;
        foreach (var t in cfg.Schedule.DailyTimes)
        {
            if (TimeSpan.TryParse(t, out var tod))
            {
                var candidate = now.Date + tod;
                if (candidate <= now) candidate = candidate.AddDays(1);
                if (next == null || candidate < next) next = candidate;
            }
        }
        if (next == null) return;
        var due = next.Value - now + TimeSpan.FromMinutes(cfg.Schedule.InitialDelayMinutes);
        _dailyTimer = new Timer(async _ =>
        {
            await RunBackupAsync(CancellationToken.None, reason: "DailyTimes");
            ScheduleNextDaily();
        }, null, due, Timeout.InfiniteTimeSpan);
        Log.Information("Next backup scheduled at {Next} ({TZ})", next, cfg.Schedule.TimeZone);
    }

    private async Task ApplyRetentionAsync(CancellationToken ct)
    {
        var cfg = _settings.CurrentValue;
        var provider = _factory.Create(_dbType);
        var list = await provider.ListAsync(ct);
        var maxAge = TimeSpan.FromDays(cfg.Storage.Retention.MaxAgeDays);
        var toDelete = list
            .OrderByDescending(b => b.CreatedUtc)
            .Skip(cfg.Storage.Retention.MaxCount)
            .Concat(list.Where(b => DateTime.UtcNow - b.CreatedUtc > maxAge))
            .DistinctBy(b => b.FilePath)
            .ToList();
        foreach (var b in toDelete)
        {
            try { await provider.DeleteAsync(b.FilePath, ct); Log.Information("Deleted old backup: {File}", b.FilePath); }
            catch (Exception ex) { Log.Warning(ex, "Failed to delete old backup: {File}", b.FilePath); }
        }
    }
}

