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