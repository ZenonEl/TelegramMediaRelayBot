// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Models;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

public class RetryPolicyManager : IRetryPolicyManager
{
    private readonly List<RetryPolicyConfig> _policies;

    public RetryPolicyManager(IOptionsMonitor<DownloaderConfigRoot> configMonitor)
    {
        // Сортируем политики, чтобы "*" всегда был последним
        _policies = configMonitor.CurrentValue.RetryPolicies
            .OrderBy(p => p.ErrorPatterns.Contains("*") ? 1 : 0)
            .ToList();
        
        // TODO: Подписаться на configMonitor.OnChange для обновления списка политик при Hot Reload
    }

    public RetryDecision Decide(DownloadResult lastResult, int attemptNumber)
    {
        // 1. Ищем первое подходящее правило
        var policy = FindMatchingPolicy(lastResult.ErrorMessage ?? string.Empty);

        if (policy == null)
        {
            // Если правил нет, никогда не повторяем
            return new RetryDecision { ShouldRetry = false };
        }

        // 2. Проверяем, не превысили ли мы количество попыток
        if (attemptNumber >= policy.MaxAttempts)
        {
            Log.Debug("Retry limit reached for policy '{PolicyName}'.", policy.Name);
            return new RetryDecision { ShouldRetry = false, Reason = "Retry limit reached." };
        }

        // 3. Формируем "модификатор" для следующей попытки
        NextAttemptModifiers? modifiers = null;
        if (policy.Action == RetryAction.UseProxy && !string.IsNullOrEmpty(policy.ProxyName))
        {
            modifiers = new NextAttemptModifiers { UseProxyName = policy.ProxyName };
        }

        // 4. Принимаем решение о повторной попытке
        Log.Information("Applying retry policy '{PolicyName}'. Attempt {Attempt}/{MaxAttempts}. Delay: {Delay}s.", 
            policy.Name, attemptNumber, policy.MaxAttempts, policy.Delay.TotalSeconds);
        
        return new RetryDecision
        {
            ShouldRetry = true,
            Delay = policy.Delay,
            Modifiers = modifiers,
            Reason = $"Policy '{policy.Name}' triggered."
        };
    }

    private RetryPolicyConfig? FindMatchingPolicy(string errorMessage)
    {
        foreach (var policy in _policies)
        {
            foreach (var pattern in policy.ErrorPatterns)
            {
                if (pattern == "*")
                {
                    // Правило "*" совпадает всегда
                    return policy;
                }
                if (errorMessage.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    // Нашли совпадение по подстроке
                    return policy;
                }
            }
        }
        return null;
    }
}