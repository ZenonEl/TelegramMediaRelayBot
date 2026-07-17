// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

namespace TelegramMediaRelayBot
{
    public static class DownloadQueue
    {
        private static SemaphoreSlim _semaphore = null!;
        private static int _queuedCount = 0;

        public static void Initialize(int maxConcurrent)
        {
            _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        }

        public static async Task<T?> EnqueueAsync<T>(
            Func<Task<T?>> downloadFunc,
            Action<int>? onQueued = null,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _queuedCount);
            bool acquired = false;
            try
            {
                if (_semaphore.CurrentCount == 0)
                    onQueued?.Invoke(_queuedCount);

                await _semaphore.WaitAsync(ct);
                acquired = true;
                Interlocked.Decrement(ref _queuedCount);

                return await downloadFunc();
            }
            finally
            {
                // Only release a permit we actually took; releasing when WaitAsync was
                // cancelled before acquiring would inflate the semaphore and break the cap.
                if (acquired) _semaphore.Release();
                else Interlocked.Decrement(ref _queuedCount);
            }
        }

        public static int QueuedCount => _queuedCount;
        public static int ActiveCount => Config.maxConcurrentDownloads - _semaphore.CurrentCount;
    }
}
