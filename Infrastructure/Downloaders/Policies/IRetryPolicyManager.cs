// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Domain.Models;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

/// <summary>
/// Представляет решение, принятое Политикой Рет-раев.
/// </summary>
public class RetryDecision
{
    /// <summary>
    /// Следует ли выполнить повторную попытку.
    /// </summary>
    public bool ShouldRetry { get; init; }

    /// <summary>
    /// Задержка перед следующей попыткой.
    /// </summary>
    public TimeSpan Delay { get; init; }

    /// <summary>
    /// (Задел на будущее) Сообщение для пользователя, объясняющее причину.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Модификатор для следующей попытки.
    /// Например, указание использовать определенный прокси.
    /// </summary>
    public NextAttemptModifiers? Modifiers { get; set; }
}

/// <summary>
/// Описывает, как нужно изменить следующую попытку.
/// </summary>
public class NextAttemptModifiers
{
    /// <summary>
    /// Указывает, какой прокси следует использовать.
    /// </summary>
    public string? UseProxyName { get; set; }
}


/// <summary>
/// Принимает решение о необходимости повторной попытки скачивания.
/// </summary>
public interface IRetryPolicyManager
{
    /// <summary>
    /// Анализирует результат и номер попытки, чтобы решить, что делать дальше.
    /// </summary>
    /// <param name="lastResult">Результат последней неудачной попытки.</param>
    /// <param name="attemptNumber">Номер текущей попытки (начиная с 1).</param>
    /// <returns>Объект RetryDecision с решением.</returns>
    RetryDecision Decide(DownloadResult lastResult, int attemptNumber);
}