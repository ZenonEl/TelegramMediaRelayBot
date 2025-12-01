// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Config.Downloaders;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;

/// <summary>
/// Принимает решение о том, какой прокси-сервер использовать для попытки скачивания.
/// </summary>
public interface IProxyPolicyManager
{
    /// <summary>
    /// Определяет, какой прокси использовать, на основе конфигурации загрузчика и URL.
    /// </summary>
    /// <param name="downloaderConfig">Конфигурация конкретного загрузчика.</param>
    /// <param name="url">URL для скачивания.</param>
    /// <returns>Адрес прокси-сервера или null, если прокси не нужен.</returns>
    string? GetProxyAddress(DownloaderDefinition downloaderConfig, string url);
}
