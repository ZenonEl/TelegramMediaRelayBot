// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TelegramMediaRelayBot.Infrastructure.Backup
{
    public class BackupHostedService : IHostedService
    {
        private readonly IBackupOrchestrator _backupOrchestrator;
        private readonly ILogger<BackupHostedService> _logger;

        public BackupHostedService(IBackupOrchestrator backupOrchestrator, ILogger<BackupHostedService> logger)
        {
            _backupOrchestrator = backupOrchestrator;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backup Hosted Service is starting.");
            await _backupOrchestrator.InitializeAsync(cancellationToken);
            await _backupOrchestrator.RunOnStartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backup Hosted Service is stopping, running shutdown backup...");

            // For a critical operation like shutdown backup, we might ignore the host's cancellation token
            // if we want to ensure it runs to completion as much as possible.
            await _backupOrchestrator.RunOnShutdownAsync(CancellationToken.None);

            _logger.LogInformation("Shutdown backup completed.");
        }
    }
}
