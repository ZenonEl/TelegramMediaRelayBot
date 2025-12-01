// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

/// <summary>
/// Defines a Telegram callback query handler.
/// Implementations handle a specific callback command identified by <see cref="Name"/>.
/// </summary>
public interface IBotCallbackQueryHandlers
{
    /// <summary>
    /// Unique prefix or full name of the callback command this handler processes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the handler logic for the provided callback update.
    /// </summary>
    Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct);

}
