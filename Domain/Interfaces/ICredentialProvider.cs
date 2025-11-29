// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Domain.Interfaces;

public interface ICredentialProvider
{
    /// <summary>
    /// Возвращает полный путь к файлу кук, проверяя папку Secrets.
    /// </summary>
    string? GetCookieFilePath(string? fileName);

    /// <summary>
    /// Разрешает секрет (заменяет ${ENV_VAR} на значение).
    /// </summary>
    string? ResolveSecret(string? value);
}