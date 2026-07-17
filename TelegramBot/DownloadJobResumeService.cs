// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Microsoft.Extensions.Hosting;
using TelegramMediaRelayBot.Database.Interfaces;

namespace TelegramMediaRelayBot.TelegramBot;

/// <summary>
/// Re-enqueues download jobs that were persisted but not completed before the
/// last shutdown/crash, so a reboot never loses requested downloads.
/// </summary>
public class DownloadJobResumeService : BackgroundService
{
    private static readonly TimeSpan StartupGrace = TimeSpan.FromSeconds(3);

    private readonly ITelegramBotClient _botClient;
    private readonly TGBot _tgBot;
    private readonly IDownloadJobRepository _jobRepository;

    public DownloadJobResumeService(ITelegramBotClient botClient, TGBot tgBot, IDownloadJobRepository jobRepository)
    {
        _botClient = botClient;
        _tgBot = tgBot;
        _jobRepository = jobRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupGrace, stoppingToken);

            var jobs = _jobRepository.GetAll();
            if (jobs.Count == 0) return;

            Log.Information("Resuming {Count} unfinished download job(s) after restart", jobs.Count);

            // Enqueue all jobs concurrently; DownloadQueue enforces the actual
            // download concurrency limit.
            var tasks = jobs.Select(job => ResumeJobAsync(job, stoppingToken)).ToList();
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // shutdown during grace delay
        }
    }

    private async Task ResumeJobAsync(DownloadJob job, CancellationToken ct)
    {
        try
        {
            Message statusMessage = await _botClient.SendMessage(
                job.ChatId,
                Localization.Get("ResumingDownloadAfterRestart"),
                cancellationToken: ct);

            await _tgBot.HandleMediaRequest(
                _botClient, job.Url, job.ChatId, statusMessage,
                job.TargetUserIds, job.IsGroupChat, job.Caption,
                persistedJobId: job.Id);
        }
        catch (OperationCanceledException)
        {
            // shutdown; the job row stays for the next start
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to resume download job {JobId} ({Url})", job.Id, job.Url);
        }
    }
}
