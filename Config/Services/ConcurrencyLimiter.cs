// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;

namespace TelegramMediaRelayBot.Infrastructure.Services;

public interface IConcurrencyLimiter
{
    Task<IDisposable> AcquireAsync(CancellationToken ct);
}

public class ConcurrencyLimiter : IConcurrencyLimiter
{
    private readonly SemaphoreSlim _semaphore;

    public ConcurrencyLimiter(IOptions<DownloaderConfigRoot> config)
    {
        int max = config.Value.MediaProcessing.MaxConcurrentProcessings;
        if (max <= 0) max = 1;

        _semaphore = new SemaphoreSlim(max, max);
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        return new ReleaseWrapper(_semaphore);
    }

    private class ReleaseWrapper : IDisposable
    {
        private readonly SemaphoreSlim _sem;
        private bool _disposed;

        public ReleaseWrapper(SemaphoreSlim sem) => _sem = sem;

        public void Dispose()
        {
            if (!_disposed)
            {
                _sem.Release();
                _disposed = true;
            }
        }
    }
}
