// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

namespace TelegramMediaRelayBot.TelegramBot.Handlers;

public class CallbackQueryHandlersFactory
{
    private readonly IServiceProvider _provider;

    public CallbackQueryHandlersFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string? data = update.CallbackQuery?.Data;
        if (string.IsNullOrEmpty(data)) return;

        using IServiceScope scope = _provider.CreateScope();
        IEnumerable<IBotCallbackQueryHandlers> handlers = scope.ServiceProvider.GetServices<IBotCallbackQueryHandlers>();

        IBotCallbackQueryHandlers? handler = handlers
            .Where(h => data.StartsWith(h.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.Name.Length)
            .FirstOrDefault();

        if (handler == null)
        {
            throw new Exception($"CallbackQuery handler not found for data: {data}");
        }

        await handler.ExecuteAsync(update, botClient, ct);
    }
}
