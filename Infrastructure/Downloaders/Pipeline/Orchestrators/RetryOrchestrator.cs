// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Infrastructure.Downloaders.Pipeline;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

namespace TelegramMediaRelayBot.Infrastructure.Pipeline.Orchestrators;

public class RetryOrchestrator : IRetryOrchestrator
{
    private readonly IRetryPolicyManager _retryManager;
    private readonly IProxyPolicyManager _proxyManager; // TODO Нам нужно знать прокси для смены

    public RetryOrchestrator(
        IRetryPolicyManager retryManager,
        IProxyPolicyManager proxyManager)
    {
        _retryManager = retryManager;
        _proxyManager = proxyManager;
    }

    public async Task<ExecutionResult> ExecuteWithRetriesAsync(
        IDownloadExecutor executor,
        DownloadContext context,
        CancellationToken ct)
    {
        ExecutionResult lastResult = ExecutionResult.Success();
        int attempt = 1;
        bool keepTrying = true;

        // 1. Инициализация первичного прокси (если задан в конфиге загрузчика)
        // Примечание: тут нам нужно будет прокинуть DownloaderDefinition,
        // но пока предположим, что прокси уже настроен в контексте или будет настроен в первой итерации.

        while (keepTrying && !ct.IsCancellationRequested)
        {
            context.Log($"Starting attempt #{attempt}. Proxy: {context.ActiveProxyUrl ?? "None"}");

            try
            {
                lastResult = await executor.ExecuteAsync(context, ct);

                if (lastResult.Status == ExecutionStatus.Success)
                {
                    context.Log("Execution successful.");
                    return lastResult;
                }

                if (lastResult.Status == ExecutionStatus.FatalError ||
                    lastResult.Status == ExecutionStatus.ContentNotSupported)
                {
                    context.Log($"Execution stopped. Status: {lastResult.Status}. Error: {lastResult.ErrorMessage}");
                    return lastResult;
                }
            }
            catch (Exception ex)
            {
                lastResult = ExecutionResult.Retryable($"Unhandled exception: {ex.Message}");
                context.Log($"Unhandled exception in attempt #{attempt}: {ex.Message}");
            }

            // --- АНАЛИЗ ОШИБКИ И РЕШЕНИЕ О РЕТРАЕ ---
            // Превращаем наш ExecutionResult в то, что понимает старый менеджер политик
            // (В будущем можно переписать менеджер политик на новые типы)
            var legacyResult = new Domain.Models.DownloadResult
            {
                Success = false,
                ErrorMessage = lastResult.ErrorMessage
            };

            var decision = _retryManager.Decide(legacyResult, attempt);

            if (!decision.ShouldRetry)
            {
                context.Log($"Retry policy decided to stop. Reason: {decision.Reason}");
                keepTrying = false;
            }
            else
            {
                context.Log($"Retrying... Reason: {decision.Reason}. Delay: {decision.Delay.TotalSeconds}s");

                // Применяем модификаторы (смена прокси)
                if (decision.Modifiers?.UseProxyName != null)
                {
                    // Тут мы должны получить адрес прокси по имени.
                    // Нам придется чуть расширить ProxyPolicyManager, чтобы он умел отдавать адрес по имени
                    // Или просто в контекст писать имя, а экзекьютор сам разберется.
                    // Пока упростим: считаем что Modifiers дает имя, а мы ищем адрес.
                    // context.ActiveProxyUrl = _proxyManager.GetAddressByName(decision.Modifiers.UseProxyName);
                    // (Предположим, что мы реализуем этот метод позже)
                    context.Log($"Switched proxy to: {decision.Modifiers.UseProxyName}");
                }

                if (lastResult.SuggestSwitchProxy)
                {
                     // Логика ротации прокси, если экзекьютор сам попросил
                     // context.ActiveProxyUrl = ... get random proxy ...
                }

                attempt++;
                await Task.Delay(decision.Delay, ct);
            }
        }

        return lastResult;
    }
}
